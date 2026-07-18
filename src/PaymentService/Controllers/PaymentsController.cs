using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using PaymentService.Services.Payments;

namespace PaymentService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/payments")]
public class PaymentsController(IPaymentManager paymentManager) : ControllerBase
{
    /// <summary>建立（或重用）Stripe Checkout Session。僅限內部服務（OrderService）以 service token 呼叫。</summary>
    /// <param name="request">訂單 ID、買家資訊、金額與商品名稱。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("checkout-session")]
    [Authorize(Policy = "InternalService")]
    [ProducesResponseType<CheckoutSessionResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateCheckoutSession(
        CreateCheckoutSessionRequest request, CancellationToken ct)
    {
        var result = await paymentManager.CreateCheckoutSessionAsync(request, ct);
        return StatusCode(201, result);
    }

    /// <summary>作廢訂單既有的 Stripe Checkout Session（取消訂單前呼叫，付款頁立即失效）。
    /// 訂單已完成付款回 409（呼叫端應拒絕取消）；無 Pending 付款時冪等視為成功。
    /// 僅限內部服務（OrderService）以 service token 呼叫。</summary>
    /// <param name="orderId">訂單 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("expire-by-order/{orderId:guid}")]
    [Authorize(Policy = "InternalService")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExpireByOrder(Guid orderId, CancellationToken ct)
    {
        await paymentManager.ExpireCheckoutByOrderAsync(orderId, ct);
        return NoContent();
    }

    /// <summary>查詢付款紀錄。僅 Admin 可操作。</summary>
    /// <param name="id">付款 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await paymentManager.GetAsync(id, ct));
    }
}
