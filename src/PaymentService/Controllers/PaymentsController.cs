using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using PaymentService.Services.Payments;
using Shared.Auth;
using Shared.Exceptions;

namespace PaymentService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/payments")]
public class PaymentsController(IPaymentManager paymentManager, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpPost("checkout-session")]
    [AllowAnonymous]
    [ProducesResponseType<CheckoutSessionResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateCheckoutSession(
        CreateCheckoutSessionRequest request, CancellationToken ct)
    {
        var result = await paymentManager.CreateCheckoutSessionAsync(request, currentUser.UserId, ct);
        return StatusCode(201, result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await paymentManager.GetAsync(id, ct));
    }
}
