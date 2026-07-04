using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentService.Data;
using PaymentService.Data.Entities;
using PaymentService.Models;
using PaymentService.Options;
using Shared.Exceptions;
using Stripe;
using Stripe.Checkout;

namespace PaymentService.Services.Payments;

public class PaymentManager(
    PaymentDbContext db,
    IMapper mapper,
    IOptions<StripeOptions> stripeOptions,
    AuditLogPublisher auditLog) : IPaymentManager
{
    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request, CancellationToken ct)
    {
        // 同一訂單已有未過期的 Pending 付款時直接重用，避免使用者重複建立 Checkout Session（如重複點擊購買鈕）。
        var existing = await db.Payments
            .Where(p => p.OrderId == request.OrderId
                && p.Status == PaymentStatus.Pending
                && p.ExpiresAt > DateTimeOffset.UtcNow
                && p.CheckoutUrl != null)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            return new CheckoutSessionResponse
            {
                PaymentId = existing.Id,
                SessionId = existing.ProviderCheckoutId ?? "",
                Url = existing.CheckoutUrl!,
            };
        }

        var opts = stripeOptions.Value;
        StripeConfiguration.ApiKey = opts.SecretKey;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            UserId = request.UserId,
            Email = request.Email,
            Amount = request.Amount,
            Currency = request.Currency.ToLowerInvariant(),
            Status = PaymentStatus.Pending,
        };

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = opts.SuccessUrl,
            CancelUrl = opts.CancelUrl,
            CustomerEmail = request.Email,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = request.Amount,
                        Currency = request.Currency.ToLowerInvariant(),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.ProductName,
                        },
                    },
                    Quantity = 1,
                },
            ],
            Metadata = new Dictionary<string, string>
            {
                ["payment_id"] = payment.Id.ToString(),
                ["order_id"] = request.OrderId.ToString(),
            },
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        payment.ProviderCheckoutId = session.Id;
        payment.ProviderPaymentId = session.PaymentIntentId;
        payment.CheckoutUrl = session.Url;
        payment.ExpiresAt = new DateTimeOffset(session.ExpiresAt, TimeSpan.Zero);

        db.Payments.Add(payment);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Created,
            ProviderTransactionId = session.Id,
        });

        auditLog.Add(
            who: request.UserId,
            action: "payment.checkout.created",
            target: "Payment",
            targetId: payment.Id,
            tenant: null);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // race condition：另一個並行請求已搶先建立同訂單的 Pending 付款，重用對方的 Checkout Session。
            var winner = await db.Payments
                .Where(p => p.OrderId == request.OrderId && p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (winner == null) throw;

            return new CheckoutSessionResponse
            {
                PaymentId = winner.Id,
                SessionId = winner.ProviderCheckoutId ?? "",
                Url = winner.CheckoutUrl ?? "",
            };
        }

        return new CheckoutSessionResponse
        {
            PaymentId = payment.Id,
            SessionId = session.Id,
            Url = session.Url,
        };
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505";

    public async Task<PaymentResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Payment not found.");

        return mapper.Map<PaymentResponse>(payment);
    }
}
