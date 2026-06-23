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
        CreateCheckoutSessionRequest request, Guid? userId, CancellationToken ct)
    {
        var opts = stripeOptions.Value;
        StripeConfiguration.ApiKey = opts.SecretKey;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            UserId = userId,
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

        db.Payments.Add(payment);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Created,
            ProviderTransactionId = session.Id,
        });

        auditLog.Add(
            who: userId,
            action: "payment.checkout.created",
            target: "Payment",
            targetId: payment.Id,
            tenant: null);

        await db.SaveChangesAsync(ct);

        return new CheckoutSessionResponse
        {
            PaymentId = payment.Id,
            SessionId = session.Id,
            Url = session.Url,
        };
    }

    public async Task<PaymentResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Payment not found.");

        return mapper.Map<PaymentResponse>(payment);
    }
}
