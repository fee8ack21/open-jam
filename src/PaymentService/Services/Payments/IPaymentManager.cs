using PaymentService.Models;

namespace PaymentService.Services.Payments;

public interface IPaymentManager
{
    Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request, Guid? userId, CancellationToken ct);
    Task<PaymentResponse> GetAsync(Guid id, CancellationToken ct);
}
