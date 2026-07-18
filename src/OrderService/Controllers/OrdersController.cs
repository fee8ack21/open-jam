using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services.Orders;
using Shared.Auth;
using Shared.Exceptions;

namespace OrderService.Controllers;

/// <summary>訂單 API：結帳建立、查詢、列表、取消。付款完成由 PaymentSucceededEvent 自動履約。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/orders")]
public class OrdersController(IOrderManager orderManager, ICurrentUserAccessor currentUser) : ControllerBase
{
    /// <summary>建立訂單（結帳）。消費者免註冊，憑 Email 即可下單；回應含 Stripe Checkout 付款頁 URL。</summary>
    /// <param name="request">買家 Email、貨幣與訂單項目。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var order = await orderManager.CreateAsync(request, currentUser.UserId, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id, version = "1.0" }, order);
    }

    /// <summary>查詢訂單完整資訊（含項目與狀態歷程）。</summary>
    /// <param name="id">訂單 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> Get(Guid id, CancellationToken ct) =>
        Ok(MaskForAnonymous(await orderManager.GetAsync(id, ct)));

    /// <summary>以訂單編號查詢訂單完整資訊。</summary>
    /// <param name="orderNumber">人類可讀訂單編號（如 OJ-20260624-1A2B3C4D）。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("by-number/{orderNumber}")]
    [AllowAnonymous]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> GetByNumber(string orderNumber, CancellationToken ct) =>
        Ok(MaskForAnonymous(await orderManager.GetByNumberAsync(orderNumber, ct)));

    /// <summary>查詢登入使用者本人的訂單列表（分頁），含成為會員前以同信箱訪客結帳的訂單。</summary>
    /// <param name="request">查詢條件（狀態 + 分頁）；買家身分取自登入 token（帳號 ID 與信箱）。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("mine")]
    [ProducesResponseType<ListOrdersResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListOrdersResponse>> ListMine([FromQuery] ListOrdersRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        return Ok(await orderManager.ListMineAsync(userId, currentUser.Email, request, ct));
    }

    /// <summary>查詢指定商店收到的訂單列表（賣家視角，分頁）。僅該商店 Owner 可操作。</summary>
    /// <param name="storeId">商店 ID（賣方）。</param>
    /// <param name="request">查詢條件（買家 / 狀態 + 分頁）；商店 ID 取自路由。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("store/{storeId:guid}")]
    [Authorize]
    [ProducesResponseType<ListOrdersResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListOrdersResponse>> ListByStore(Guid storeId, [FromQuery] ListOrdersRequest request, CancellationToken ct) =>
        Ok(await orderManager.ListByStoreAsync(storeId, request, ct));

    /// <summary>查詢全部訂單列表（含各狀態），可依商店 / 買家 / 狀態過濾。僅 Admin 可操作。</summary>
    /// <param name="request">查詢條件（商店 / 買家 / 狀態 + 分頁）。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<ListOrdersResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListOrdersResponse>> List([FromQuery] ListOrdersRequest request, CancellationToken ct) =>
        Ok(await orderManager.ListAsync(request, ct));

    /// <summary>查詢登入使用者是否已購買（已完成訂單）某商品。供評論前的購買驗證。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("purchased/{catalogId:guid}")]
    [ProducesResponseType<PurchaseCheckResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PurchaseCheckResponse>> HasPurchased(Guid catalogId, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var purchased = await orderManager.HasPurchasedAsync(catalogId, userId, currentUser.Email, ct);
        return Ok(new PurchaseCheckResponse { Purchased = purchased });
    }

    /// <summary>取消未付款的訂單。具名買家僅本人可取消；匿名訂單憑訂單 ID 取消。</summary>
    /// <param name="id">訂單 ID。</param>
    /// <param name="request">取消原因（選填）。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/cancel")]
    [AllowAnonymous]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> Cancel(Guid id, CancelOrderRequest request, CancellationToken ct) =>
        Ok(await orderManager.CancelAsync(id, request, currentUser.UserId, ct));

    /// <summary>
    /// 匿名呼叫者（creator-web 憑 orderId 下載的訪客）遮罩買家 email 後回傳；帶 token 者
    /// （買家本人 / 賣家 / Admin）給完整值。orderId 是下載憑證、可能隨訂單完成信外流或留在
    /// 瀏覽器歷史，不應連帶讓第三方讀到買家 PII。
    /// </summary>
    private OrderResponse MaskForAnonymous(OrderResponse order)
    {
        if (currentUser.UserId is null)
            order.BuyerEmail = MaskEmail(order.BuyerEmail);
        return order;
    }

    /// <summary>把 email 遮罩為「首字 + *** + @網域」（如 <c>b***@example.com</c>）。</summary>
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 0 ? "***" : $"{email[..1]}***{email[at..]}";
    }
}
