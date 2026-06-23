using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentService.Data;
using PaymentService.Data.Entities;
using PaymentService.Options;
using Shared.Events;
using Stripe;

namespace PaymentService.Services.Payments;

public class StripeWebhookHandler(
    PaymentDbContext db,
    IOptions<StripeOptions> stripeOptions,
    ILogger<StripeWebhookHandler> logger)
{
    public async Task<string> HandleAsync(string body, string signature, CancellationToken ct)
    {
        var opts = stripeOptions.Value;
        StripeConfiguration.ApiKey = opts.SecretKey;

        var evt = EventUtility.ConstructEvent(body, signature, opts.WebhookSecret);
        var eventId = evt.Id;
        var eventType = evt.Type;

        logger.LogInformation("Received Stripe webhook: {EventId} {EventType}", eventId, eventType);

        var existing = await db.ProviderEvents
            .FirstOrDefaultAsync(e => e.Provider == "stripe" && e.EventId == eventId, ct);

        if (existing != null)
        {
            logger.LogInformation("Duplicate webhook skipped: {EventId}", eventId);
            return evt.Type;
        }

        var providerEvent = new ProviderEvent
        {
            Id = Guid.NewGuid(),
            Provider = "stripe",
            EventId = eventId,
            EventType = eventType,
        };
        db.ProviderEvents.Add(providerEvent);

        switch (eventType)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompletedAsync(evt, ct);
                break;
            case "checkout.session.async_payment_succeeded":
                await HandleCheckoutCompletedAsync(evt, ct);
                break;
            case "checkout.session.async_payment_failed":
                await HandleCheckoutFailedAsync(evt, ct);
                break;
            case "payment_intent.payment_failed":
                await HandlePaymentFailedAsync(evt, ct);
                break;
            default:
                logger.LogInformation("Unhandled webhook type: {EventType}", eventType);
                break;
        }

        providerEvent.ProcessedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return evt.Type;
    }

    private async Task HandleCheckoutCompletedAsync(Event evt, CancellationToken ct)
    {
        var session = evt.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var paymentId = GetPaymentId(session);
        if (paymentId == null) return;

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId.Value, ct);
        if (payment == null) return;

        if (payment.Status == PaymentStatus.Succeeded) return;

        payment.Status = PaymentStatus.Succeeded;
        payment.ProviderPaymentId = session.PaymentIntentId ?? payment.ProviderPaymentId;
        payment.PaidAt = DateTimeOffset.UtcNow;

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Success,
            ProviderTransactionId = session.PaymentIntentId ?? session.Id,
            RawPayload = evt.Data.RawObject?.ToJson(),
        });

        var outbox = new OutboxMessage
        {
            EventType = "payment.succeeded",
            Payload = JsonSerializer.Serialize(new PaymentSucceededEvent(
                OutboxMessageId: Guid.NewGuid(),
                PaymentId: payment.Id,
                OrderId: payment.OrderId,
                UserId: payment.UserId,
                Email: payment.Email,
                Amount: payment.Amount,
                Currency: payment.Currency,
                ProviderPaymentId: payment.ProviderPaymentId ?? "",
                PaidAt: payment.PaidAt ?? DateTimeOffset.UtcNow
            )),
        };
        db.OutboxMessages.Add(outbox);

        logger.LogInformation("Payment succeeded: {PaymentId} {CheckoutSessionId}", payment.Id, session.Id);
    }

    private async Task HandleCheckoutFailedAsync(Event evt, CancellationToken ct)
    {
        var session = evt.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var paymentId = GetPaymentId(session);
        if (paymentId == null) return;

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId.Value, ct);
        if (payment == null) return;

        payment.Status = PaymentStatus.Failed;
        payment.FailedAt = DateTimeOffset.UtcNow;

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Fail,
            ProviderTransactionId = session.Id,
            RawPayload = evt.Data.RawObject?.ToJson(),
        });

        logger.LogInformation("Payment failed (async): {PaymentId} {CheckoutSessionId}", payment.Id, session.Id);
    }

    private async Task HandlePaymentFailedAsync(Event evt, CancellationToken ct)
    {
        var intent = evt.Data.Object as PaymentIntent;
        if (intent == null) return;

        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.ProviderPaymentId == intent.Id || p.ProviderCheckoutId == intent.Id, ct);
        if (payment == null) return;

        payment.Status = PaymentStatus.Failed;
        payment.FailedAt = DateTimeOffset.UtcNow;

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Fail,
            ProviderTransactionId = intent.Id,
            RawPayload = evt.Data.RawObject?.ToJson(),
        });

        logger.LogInformation("Payment failed: {PaymentId} {PaymentIntentId}", payment.Id, intent.Id);
    }

    private static Guid? GetPaymentId(Stripe.Checkout.Session session)
    {
        if (session.Metadata.TryGetValue("payment_id", out var pid) && Guid.TryParse(pid, out var id))
            return id;
        return null;
    }
}
