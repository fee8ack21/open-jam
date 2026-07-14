using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Services.Payments;

namespace PaymentService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/webhook")]
public class WebhookController(StripeWebhookHandler handler) : ControllerBase
{
    [HttpPost("stripe")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> Stripe(CancellationToken ct) =>
        ReceiveAsync(handler.ReceiveAsync, ct);

    /// <summary>Connect webhook 端點（account.updated 等連接帳戶事件），與平台端點分屬不同簽章密鑰。</summary>
    [HttpPost("stripe/connect")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> StripeConnect(CancellationToken ct) =>
        ReceiveAsync(handler.ReceiveConnectAsync, ct);

    private async Task<IActionResult> ReceiveAsync(
        Func<string, string, CancellationToken, Task<string>> receive, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        try
        {
            var type = await receive(body, signature, ct);
            return Ok(new { received = true, type });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
