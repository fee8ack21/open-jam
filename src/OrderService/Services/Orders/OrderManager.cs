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
        // 已購買防呆比對用信箱（小寫）：與 HasPurchasedAsync 的 Email 準則一致。訪客無 userId，
        // 僅能以結帳信箱比對；登入買家另以 userId 比對。信箱未驗證，故對訪客為「同信箱才擋」。
        var buyerEmail = request.BuyerEmail.Trim().ToLowerInvariant();

        var catalogs = new List<CatalogServiceClient.CatalogInfo>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var catalog = await catalogClient.GetCatalogAsync(item.CatalogId, ct)
                ?? throw new ValidationException($"商品 {item.CatalogId} 不存在或未上架。");

            if (catalog.StoreId != request.StoreId)
                throw new ValidationException($"商品 {catalog.Name} 不屬於此商店。");
            if (catalog.CurrentVersion is null)
                throw new ValidationException($"商品 {catalog.Name} 尚無可購買的版本。");

            // 數位商品買過即永久擁有：已完成訂單含此商品時擋下重複購買（回 409）。
            // 訊息不附下載連結（避免任意輸入他人信箱即探知 / 取得其購買），引導至訂單完成信。
            if (await HasPurchasedAsync(item.CatalogId, userId ?? Guid.Empty, buyerEmail, ct))
                throw new ConflictException($"您已購買過「{catalog.Name}」，下載連結請見當初的訂單完成信，無需重複購買。");

            catalogs.Add(catalog);
        }

        var currency = catalogs[0].Currency.ToLowerInvariant();
        if (catalogs.Any(c => !string.Equals(c.Currency, catalogs[0].Currency, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("訂單項目的幣別不一致，請分開結帳。");

        // 重複送出防呆（如手滑連點購買）：同買家 / 同商店 / 同商品組合的近期未付款訂單直接重用，
        // 不再建新單，避免累積孤兒 Pending 訂單。重用時重呼叫 PaymentService（以 OrderId 冪等——
        // 既有 Session 未過期則沿用、否則重簽），回既有訂單的付款頁 URL。限近期（避免沿用過期定價）。
        var reusable = await FindReusablePendingOrderAsync(request, userId, buyerEmail, ct);
        if (reusable is not null)
        {
            var reuseSession = await paymentClient.CreateCheckoutSessionAsync(
                reusable.Id, reusable.StoreId, reusable.BuyerUserId, reusable.BuyerEmail,
                reusable.TotalAmount, reusable.Currency, BuildProductName(reusable), ct);

            var reuseResponse = await GetAsync(reusable.Id, ct);
            reuseResponse.CheckoutUrl = reuseSession.Url;
            return reuseResponse;
        }

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

        // 免費訂單（總額 0）：Stripe 不接受 0 元 Checkout，跳過金流直接履約完成，
        // CheckoutUrl 維持 null，前端據此不導向付款頁、直接進入訂單（下載）頁。
        if (order.TotalAmount == 0)
        {
            await FulfillAsync(order, DateTimeOffset.UtcNow, "Free order", ct);
            return await GetAsync(order.Id, ct);
        }

        // 訂單落地後向 PaymentService 建立 Checkout Session，付款頁 URL 隨建單回應交給前端導向。
        // 失敗時訂單保留 Pending（不回滾），由前端重試結帳；PaymentService 以 OrderId 去重重用既有 Session。
        var session = await paymentClient.CreateCheckoutSessionAsync(
            order.Id, order.StoreId, userId, order.BuyerEmail, order.TotalAmount, currency,
            BuildProductName(order), ct);

        var response = await GetAsync(order.Id, ct);
        response.CheckoutUrl = session.Url;
        return response;
    }

    /// <summary>重複送出去重的未付款訂單重用時效——超過此時窗的舊 Pending 單另建新單（避免沿用過期定價）。</summary>
    private static readonly TimeSpan ReusablePendingWindow = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 找出可重用的近期未付款訂單：同商店、同買家（登入 userId 或結帳信箱）、商品組合完全相同。
    /// 供「重複送出 / 連點購買」去重；回 null 表示應建立新訂單。並發的同時送出（罕見）仍可能各建一單，
    /// 屬可接受殘量（僅多一筆未付款孤兒單，不會重複扣款）。
    /// </summary>
    private async Task<Order?> FindReusablePendingOrderAsync(
        CreateOrderRequest request, Guid? userId, string buyerEmail, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow - ReusablePendingWindow;

        var query = db.Orders
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Pending
                && o.StoreId == request.StoreId
                && o.CreatedAt >= cutoff);

        // 訪客無 userId，僅以結帳信箱比對；登入買家另以 userId 比對（與 HasPurchasedAsync 準則一致）。
        query = userId is { } uid
            ? query.Where(o => o.BuyerUserId == uid || o.BuyerEmail.ToLower() == buyerEmail)
            : query.Where(o => o.BuyerEmail.ToLower() == buyerEmail);

        var candidates = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);

        // 商品組合完全相同（忽略順序）才算同一張購物車；集合比對於記憶體進行，候選數極少。
        var wanted = request.Items.Select(i => i.CatalogId).OrderBy(id => id).ToList();
        return candidates.FirstOrDefault(o =>
            o.Items.Select(i => i.CatalogId).OrderBy(id => id).SequenceEqual(wanted));
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
    public Task<ListOrdersResponse> ListAsync(ListOrdersRequest request, CancellationToken ct)
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

        return PageAsync(query, request, ct);
    }

    /// <inheritdoc/>
    public Task<ListOrdersResponse> ListMineAsync(Guid userId, string? email, ListOrdersRequest request, CancellationToken ct)
    {
        // 以結帳時實際填寫的 Email 一併比對（與 HasPurchasedAsync 同準則），
        // 讓成為會員前以同信箱訪客結帳的訂單也出現在本人購買紀錄。
        var query = email is null
            ? db.Orders.AsNoTracking().Where(o => o.BuyerUserId == userId)
            : db.Orders.AsNoTracking().Where(o => o.BuyerUserId == userId || o.BuyerEmail.ToLower() == email);

        return PageAsync(query, request, ct);
    }

    /// <summary>套用狀態過濾與分頁，輸出訂單摘要列表（各列表視角共用）。</summary>
    private async Task<ListOrdersResponse> PageAsync(IQueryable<Order> query, ListOrdersRequest request, CancellationToken ct)
    {
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

        // expire-first：先向 Stripe 作廢 Checkout Session、成功才取消訂單。付款成敗的真相在
        // Stripe——已付款的 Session 無法作廢（此呼叫拋 409），據此拒絕取消，關閉「取消後付款頁
        // 仍可付款」與「剛付完款但成功事件還在路上」兩種競態。此步失敗時訂單維持 Pending 且
        // Session 已死或未動，無金錢風險，買家重試即可補完（整條鏈冪等）。
        await paymentClient.ExpireCheckoutSessionAsync(order.Id, ct);

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

        // 已取消 / 已退款訂單收到付款成功：正常流程不應發生（取消前已作廢 Session），代表買家
        // 已被扣款但訂單不會履約——留下 error log 與稽核事件供人工介入退款，不得靜默略過。
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            logger.LogError(
                "訂單 {OrderId} 狀態為 {Status} 卻收到付款成功事件（付款時間 {PaidAt}）：買家已被扣款但訂單不會履約，需人工退款處理。",
                order.Id, order.Status, paidAt);
            auditLog.Add(order.BuyerUserId, "order.paid_after_cancel", "Order", order.Id, tenant: null);
            await db.SaveChangesAsync(ct);
            return;
        }

        // 冪等：訂單已完成時不再處理，避免重複的 PaymentSucceededEvent 造成重複狀態。
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Paid))
            return;

        await FulfillAsync(order, paidAt, "Payment succeeded", ct);
    }

    /// <summary>履約完成訂單：轉移狀態、發出完成事件與訂單完成信（付款成功與免費訂單共用）。</summary>
    private async Task FulfillAsync(Order order, DateTimeOffset completedAt, string reason, CancellationToken ct)
    {
        order.CompletedAt = completedAt;
        TransitionTo(order, OrderStatus.Completed, reason);
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
    public Task<bool> HasPurchasedAsync(Guid catalogId, Guid userId, string? email, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Where(o => o.Status == OrderStatus.Completed);

        // 以結帳時實際填寫的 Email 為主比對（訪客訂單無 BuyerUserId），帳號 ID 相符者亦視為已購買。
        query = email is null
            ? query.Where(o => o.BuyerUserId == userId)
            : query.Where(o => o.BuyerUserId == userId || o.BuyerEmail.ToLower() == email);

        return query.AnyAsync(o => o.Items.Any(i => i.CatalogId == catalogId), ct);
    }

    private void TransitionTo(Order order, OrderStatus next, string? reason)
    {
        var history = NewHistory(order.Status, next, reason);
        order.StatusHistory.Add(history);
        // 訂單已被追蹤時，集合裡的新歷程列因 Id 已有值會被 DetectChanges 當成既有列（Modified），
        // 產生打不中任何列的 UPDATE 而拋 DbUpdateConcurrencyException，故一律明確標記為新增。
        db.Add(history);
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
