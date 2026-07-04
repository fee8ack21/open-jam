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
