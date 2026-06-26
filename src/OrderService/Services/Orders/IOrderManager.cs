using OrderService.Models;

namespace OrderService.Services.Orders;

public interface IOrderManager
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, Guid? userId, CancellationToken ct);
    Task<OrderResponse> GetAsync(Guid id, CancellationToken ct);
    Task<OrderResponse> GetByNumberAsync(string orderNumber, CancellationToken ct);
    Task<ListOrdersResponse> ListAsync(ListOrdersRequest request, CancellationToken ct);

    /// <summary>查詢指定商店收到的訂單列表；先驗證呼叫者為該商店 Owner。</summary>
    Task<ListOrdersResponse> ListByStoreAsync(Guid storeId, ListOrdersRequest request, CancellationToken ct);

    Task<OrderResponse> CancelAsync(Guid id, CancelOrderRequest request, Guid? userId, CancellationToken ct);

    /// <summary>查詢指定使用者是否曾以已完成訂單購買某商品（供評論購買驗證）。</summary>
    Task<bool> HasPurchasedAsync(Guid catalogId, Guid userId, CancellationToken ct);

    /// <summary>付款成功時履約完成訂單（由 <c>PaymentSucceededEvent</c> consumer 呼叫，冪等）。</summary>
    Task CompleteFromPaymentAsync(Guid orderId, DateTimeOffset paidAt, CancellationToken ct);
}
