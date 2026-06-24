using OrderService.Models;

namespace OrderService.Services.Orders;

public interface IOrderManager
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, Guid? userId, CancellationToken ct);
    Task<OrderResponse> GetAsync(Guid id, CancellationToken ct);
    Task<OrderResponse> GetByNumberAsync(string orderNumber, CancellationToken ct);
    Task<ListOrdersResponse> ListAsync(ListOrdersRequest request, CancellationToken ct);
    Task<OrderResponse> CancelAsync(Guid id, CancelOrderRequest request, Guid? userId, CancellationToken ct);

    /// <summary>付款成功時履約完成訂單（由 <c>PaymentSucceededEvent</c> consumer 呼叫，冪等）。</summary>
    Task CompleteFromPaymentAsync(Guid orderId, DateTimeOffset paidAt, CancellationToken ct);
}
