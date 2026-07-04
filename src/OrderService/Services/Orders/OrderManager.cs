using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderService.Data;
using OrderService.Data.Entities;
using OrderService.Models;
using OrderService.Options;
using OrderService.Services;
using Shared.Exceptions;

namespace OrderService.Services.Orders;

/// <summary>訂單業務邏輯實作。</summary>
public class OrderManager(
    OrderDbContext db,
    IMapper mapper,
    StoreServiceClient storeClient,
    PaymentServiceClient paymentClient,
    CatalogServiceClient catalogClient,
    AuditLogPublisher auditLog,
    OrderEventPublisher orderEvents,
    IOptions<OrderOptions> orderOptions,
    ILogger<OrderManager> logger) : IOrderManager
{
    /// <inheritdoc/>
    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, Guid? userId, CancellationToken ct)
    {
        // 伺服器端核價：逐項向 CatalogService 取回商品現況（匿名呼叫，未上架 / 不存在回 404），
        // 名稱 / 單價 / 版本 / 幣別一律以伺服器端快照為準，不信任 client 傳入值。
        var catalogs = new List<CatalogServiceClient.CatalogInfo>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var catalog = await catalogClient.GetCatalogAsync(item.CatalogId, ct)
                ?? throw new ValidationException($"商品 {item.CatalogId} 不存在或未上架。");

            if (catalog.StoreId != request.StoreId)
                throw new ValidationException($"商品 {catalog.Name} 不屬於此商店。");
            if (catalog.CurrentVersion is null)
                throw new ValidationException($"商品 {catalog.Name} 尚無可購買的版本。");

            catalogs.Add(catalog);
        }

        var currency = catalogs[0].Currency.ToLowerInvariant();
        if (catalogs.Any(c => !string.Equals(c.Currency, catalogs[0].Currency, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("訂單項目的幣別不一致，請分開結帳。");

        var order = new Order
        {
            Id          = Guid.NewGuid(),
            OrderNumber = OrderNumberGenerator.Next(),
            StoreId     = request.StoreId,
            BuyerUserId = userId,
            BuyerEmail  = request.BuyerEmail,
            Currency    = currency,
            Status      = OrderStatus.Pending,
            Items       = catalogs.Select(c => new OrderItem
            {
                Id               = Guid.NewGuid(),
                CatalogId        = c.Id,
                CatalogVersionId = c.CurrentVersion!.Id,
                CatalogName      = c.Name,
                UnitPrice        = ToMinorUnits(c.Price),
            }).ToList(),
        };

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice);
        order.StatusHistory.Add(NewHistory(null, OrderStatus.Pending, "Order created"));

        db.Orders.Add(order);
        auditLog.Add(userId, "order.create", "Order", order.Id, tenant: null);

        await db.SaveChangesAsync(ct);

        // 訂單落地後向 PaymentService 建立 Checkout Session，付款頁 URL 隨建單回應交給前端導向。
        // 失敗時訂單保留 Pending（不回滾），由前端重試結帳；PaymentService 以 OrderId 去重重用既有 Session。
        var session = await paymentClient.CreateCheckoutSessionAsync(
            order.Id, userId, order.BuyerEmail, order.TotalAmount, currency, BuildProductName(order), ct);

        var response = await GetAsync(order.Id, ct);
        response.CheckoutUrl = session.Url;
        return response;
    }

    /// <summary>組出顯示於 Stripe Checkout 頁面的商品名稱：單品用品名，多品以首件名稱加總件數。</summary>
    private static string BuildProductName(Order order) =>
        order.Items.Count == 1
            ? order.Items[0].CatalogName
            : $"{order.Items[0].CatalogName} 等 {order.Items.Count} 件商品";

    /// <summary>商品售價（主幣單位）換算為最低貨幣單位（如 cents），與 Stripe 金額格式一致。</summary>
    private static long ToMinorUnits(decimal price) =>
        (long)Math.Round(price * 100m, MidpointRounding.AwayFromZero);

    /// <inheritdoc/>
    public Task<OrderResponse> GetAsync(Guid id, CancellationToken ct) =>
        ToResponseAsync(db.Orders.Where(o => o.Id == id), ct);

    /// <inheritdoc/>
    public Task<OrderResponse> GetByNumberAsync(string orderNumber, CancellationToken ct) =>
        ToResponseAsync(db.Orders.Where(o => o.OrderNumber == orderNumber), ct);

    /// <inheritdoc/>
    public async Task<ListOrdersResponse> ListAsync(ListOrdersRequest request, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();

        if (request.StoreId is { } storeId)
            query = query.Where(o => o.StoreId == storeId);

        if (request.BuyerUserId is { } buyerUserId)
            query = query.Where(o => o.BuyerUserId == buyerUserId);

        if (!string.IsNullOrWhiteSpace(request.BuyerEmail))
        {
            var email = request.BuyerEmail.Trim();
            query = query.Where(o => o.BuyerEmail == email);
        }

        if (request.Status is { } status)
            query = query.Where(o => o.Status == status);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ProjectTo<OrderSummaryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new ListOrdersResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task<ListOrdersResponse> ListByStoreAsync(Guid storeId, ListOrdersRequest request, CancellationToken ct)
    {
        // 賣家視角：先確認呼叫者為該商店 Owner，再以商店 ID 限縮查詢（覆寫客戶端傳入值，避免越權）。
        await storeClient.EnsureStoreOwnerAsync(storeId, ct);
        request.StoreId = storeId;
        return await ListAsync(request, ct);
    }

    /// <inheritdoc/>
    public async Task<OrderResponse> CancelAsync(Guid id, CancelOrderRequest request, Guid? userId, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("找不到訂單。");

        // 具名買家的訂單僅本人可取消；匿名（Email）訂單憑訂單 ID 即可取消。
        if (order.BuyerUserId is { } buyer && buyer != userId)
            throw new ForbiddenException("無權取消此訂單。");

        if (order.Status != OrderStatus.Pending)
            throw new ConflictException("僅未付款的訂單可取消。");

        TransitionTo(order, OrderStatus.Cancelled, request.Reason ?? "Cancelled by buyer");
        auditLog.Add(userId, "order.cancel", "Order", order.Id, tenant: null);

        await db.SaveChangesAsync(ct);

        return await GetAsync(order.Id, ct);
    }

    /// <inheritdoc/>
    public async Task CompleteFromPaymentAsync(Guid orderId, DateTimeOffset paidAt, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            throw new NotFoundException($"找不到訂單 {orderId}。");

        // 冪等：訂單已完成（或已取消 / 退款）時不再處理，避免重複的 PaymentSucceededEvent 造成重複狀態。
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Paid))
            return;

        order.CompletedAt = paidAt;
        TransitionTo(order, OrderStatus.Completed, "Payment succeeded");
        auditLog.Add(order.BuyerUserId, "order.complete", "Order", order.Id, tenant: null);
        // 同一 transaction 內發出履約完成事件（攜帶商品明細，供 CatalogService 累加銷量等）。
        orderEvents.AddOrderCompleted(order);

        // 訂單完成信（含下載頁連結）：best effort——商店資訊查詢失敗時僅記 log，不阻擋履約完成。
        try
        {
            var store = await storeClient.GetStoreAsync(order.StoreId, ct);
            var downloadUrl = orderOptions.Value.DownloadUrlPattern
                .Replace("{storeSlug}", store.StoreSlug)
                .Replace("{orderId}", order.Id.ToString());
            orderEvents.AddOrderCompletedEmail(order, store.StoreName, downloadUrl);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "訂單 {OrderId} 完成信寄送準備失敗（查詢商店 {StoreId} 資訊），僅略過寄信。",
                order.Id, order.StoreId);
        }

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public Task<bool> HasPurchasedAsync(Guid catalogId, Guid userId, CancellationToken ct) =>
        db.Orders
            .AsNoTracking()
            .Where(o => o.BuyerUserId == userId && o.Status == OrderStatus.Completed)
            .AnyAsync(o => o.Items.Any(i => i.CatalogId == catalogId), ct);

    private static void TransitionTo(Order order, OrderStatus next, string? reason)
    {
        order.StatusHistory.Add(NewHistory(order.Status, next, reason));
        order.Status = next;
    }

    private static OrderStatusHistory NewHistory(OrderStatus? oldStatus, OrderStatus newStatus, string? reason) =>
        new()
        {
            Id        = Guid.NewGuid(),
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Reason    = reason,
        };

    private async Task<OrderResponse> ToResponseAsync(IQueryable<Order> query, CancellationToken ct)
    {
        var order = await query
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("找不到訂單。");

        var response = mapper.Map<OrderResponse>(order);
        response.StatusHistory = response.StatusHistory.OrderBy(h => h.CreatedAt).ToList();
        return response;
    }
}
