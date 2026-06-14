using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreService.Models;
using StoreService.Services.Stores;

namespace StoreService.Controllers;

/// <summary>商店 API：查詢、更新基本資料、狀態管理（停權／解除停權／關閉）、Avatar/Banner 上傳。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/stores")]
[Authorize]
public class StoresController(IStoreManager storeManager) : ControllerBase
{
    /// <summary>查詢商店基本資訊（公開）。</summary>
    /// <param name="idOrSlug">商店 ID 或 StoreSlug。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{idOrSlug}")]
    [AllowAnonymous]
    [ProducesResponseType<StoreDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreDto>> GetAsync(string idOrSlug, CancellationToken ct) =>
        Ok(await storeManager.GetAsync(idOrSlug, ct));

    /// <summary>查詢登入使用者所屬的商店列表。</summary>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("me")]
    [ProducesResponseType<List<MyStoreDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MyStoreDto>>> GetMineAsync(CancellationToken ct) =>
        Ok(await storeManager.GetMineAsync(ct));

    /// <summary>更新商店基本資料（StoreName / Description）。僅 Owner 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="request">欲更新的欄位。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<StoreDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreDto>> UpdateAsync(Guid id, [FromBody] UpdateStoreRequest request, CancellationToken ct) =>
        Ok(await storeManager.UpdateAsync(id, request, ct));

    /// <summary>平台停權商店（Active → Suspended）。僅 Admin 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/suspend")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SuspendAsync(Guid id, CancellationToken ct)
    {
        await storeManager.SuspendAsync(id, ct);
        return NoContent();
    }

    /// <summary>解除商店停權（Suspended → Active）。僅 Admin 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/unsuspend")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnsuspendAsync(Guid id, CancellationToken ct)
    {
        await storeManager.UnsuspendAsync(id, ct);
        return NoContent();
    }

    /// <summary>關閉商店（Active/Suspended → Closed，終態不可逆）。Owner 或 Admin 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CloseAsync(Guid id, CancellationToken ct)
    {
        await storeManager.CloseAsync(id, ct);
        return NoContent();
    }

    /// <summary>申請商店頭像（Avatar）上傳簽章 URL。僅 Owner 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="request">檔案資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/avatar/upload-url")]
    [ProducesResponseType<AssetUploadUrlResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AssetUploadUrlResponse>> RequestAvatarUploadUrlAsync(
        Guid id, [FromBody] RequestAssetUploadUrlRequest request, CancellationToken ct) =>
        Ok(await storeManager.RequestAssetUploadUrlAsync(id, request, isAvatar: true, ct));

    /// <summary>申請商店橫幅（Banner）上傳簽章 URL。僅 Owner 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="request">檔案資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/banner/upload-url")]
    [ProducesResponseType<AssetUploadUrlResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AssetUploadUrlResponse>> RequestBannerUploadUrlAsync(
        Guid id, [FromBody] RequestAssetUploadUrlRequest request, CancellationToken ct) =>
        Ok(await storeManager.RequestAssetUploadUrlAsync(id, request, isAvatar: false, ct));
}
