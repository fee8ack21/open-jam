using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using PaymentService.Services;
using PaymentService.Services.Connect;

namespace PaymentService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/connect/accounts")]
public class ConnectAccountsController(
    IConnectAccountService connectAccounts,
    StoreServiceClient storeClient) : ControllerBase
{
    /// <summary>為商店建立（或重用）Stripe Express 帳戶並簽發 onboarding Account Link。僅商店 Owner 可操作。</summary>
    /// <param name="storeId">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{storeId:guid}/onboarding-link")]
    [Authorize]
    [ProducesResponseType<AccountLinkResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AccountLinkResponse>> CreateOnboardingLink(Guid storeId, CancellationToken ct)
    {
        await storeClient.EnsureStoreOwnerAsync(storeId, ct);
        var result = await connectAccounts.CreateOnboardingLinkAsync(storeId, ct);
        return StatusCode(201, result);
    }

    /// <summary>簽發商店 Stripe Express Dashboard 登入連結（短效，前端直接開啟）。僅商店 Owner 可操作；
    /// 創作者於 Stripe 託管頁查看餘額、撥款排程與交易明細。</summary>
    /// <param name="storeId">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{storeId:guid}/login-link")]
    [Authorize]
    [ProducesResponseType<AccountLinkResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AccountLinkResponse>> CreateLoginLink(Guid storeId, CancellationToken ct)
    {
        await storeClient.EnsureStoreOwnerAsync(storeId, ct);
        var result = await connectAccounts.CreateLoginLinkAsync(storeId, ct);
        return StatusCode(201, result);
    }

    /// <summary>查詢商店連接帳戶狀態。僅商店 Owner 可操作；<paramref name="refresh"/> 時向 Stripe 取即時狀態。</summary>
    /// <param name="storeId">商店 ID。</param>
    /// <param name="refresh">是否向 Stripe 取得即時狀態並回寫（onboarding 導回頁使用）。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{storeId:guid}")]
    [Authorize]
    [ProducesResponseType<ConnectAccountStatusResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConnectAccountStatusResponse>> Get(
        Guid storeId, [FromQuery] bool refresh, CancellationToken ct)
    {
        await storeClient.EnsureStoreOwnerAsync(storeId, ct);
        return Ok(await connectAccounts.GetStatusAsync(storeId, refresh, ct));
    }

    /// <summary>查詢商店收款狀態（僅布林旗標）。匿名公開，供 CatalogService 上架閘門與前端顯示。</summary>
    /// <param name="storeId">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{storeId:guid}/status")]
    [AllowAnonymous]
    [ProducesResponseType<ConnectAccountStatusResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConnectAccountStatusResponse>> GetStatus(Guid storeId, CancellationToken ct)
    {
        return Ok(await connectAccounts.GetStatusAsync(storeId, refresh: false, ct));
    }
}
