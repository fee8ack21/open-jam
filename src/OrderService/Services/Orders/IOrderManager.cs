using OrderService.Models;

namespace OrderService.Services.Orders;

public interface IOrderManager
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, Guid? userId, CancellationToken ct);
    Task<OrderResponse> GetAsync(Guid id, CancellationToken ct);
    Task<OrderResponse> GetByNumberAsync(string orderNumber, CancellationToken ct);
    Task<ListOrdersResponse> ListAsync(ListOrdersRequest request, CancellationToken ct);

    /// <summary>
    /// 查詢登入使用者本人的訂單列表。除帳號 ID 外，亦納入結帳時填寫同信箱的訪客訂單
    /// （<paramref name="email"/> 須為小寫），讓成為會員前的購買紀錄也看得到。
    /// </summary>
    Task<ListOrdersResponse> ListMineAsync(Guid userId, string? email, ListOrdersRequest request, CancellationToken ct);

    /// <summary>查詢指定商店收到的訂單列表；先驗證呼叫者為該商店 Owner。</summary>
    Task<ListOrdersResponse> ListByStoreAsync(Guid storeId, ListOrdersRequest request, CancellationToken ct);

    Task<OrderResponse> CancelAsync(Guid id, CancelOrderRequest request, Guid? userId, CancellationToken ct);

    /// <summary>
    /// 查詢指定使用者是否曾以已完成訂單購買某商品（供評論購買驗證）。
    /// 訪客結帳的訂單無 BuyerUserId，故另以結帳時實際填寫的 Email 比對（<paramref name="email"/> 須為小寫）。
    /// </summary>
    Task<bool> HasPurchasedAsync(Guid catalogId, Guid userId, string? email, CancellationToken ct);

    /// <summary>付款成功時履約完成訂單（由 <c>PaymentSucceededEvent</c> consumer 呼叫，冪等）。</summary>
    Task CompleteFromPaymentAsync(Guid orderId, DateTimeOffset paidAt, CancellationToken ct);
}
