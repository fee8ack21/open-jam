using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Auth;
using Shared.Events;
using Shared.Exceptions;
using StoreService.Data;
using StoreService.Data.Entities;
using StoreService.Models;
using StoreService.Services;

namespace StoreService.Controllers;

/// <summary>商店 API：查詢、更新基本資料、狀態管理（停權／解除停權／關閉）、Avatar/Banner 上傳。</summary>
[ApiController]
[Route("stores")]
[Authorize]
public class StoresController(
    StoreDbContext db,
    ICurrentUserAccessor currentUser,
    IConfiguration config,
    StorageServiceClient storageClient) : ControllerBase
{
    private static readonly HashSet<string> AllowedImageContentTypes =
        new(StringComparer.Ordinal) { "image/jpeg", "image/png", "image/gif", "image/webp" };

    private readonly string _publicBaseUrl = (config["Storage:PublicBaseUrl"] ?? "").TrimEnd('/');

    /// <summary>查詢商店基本資訊（公開）。</summary>
    /// <param name="idOrSlug">商店 ID 或 StoreSlug。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{idOrSlug}")]
    [AllowAnonymous]
    [ProducesResponseType<StoreDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreDto>> GetAsync(string idOrSlug, CancellationToken ct)
    {
        var store = await FindStoreAsync(idOrSlug, ct)
            ?? throw new NotFoundException("找不到商店。");

        return Ok(await ToDtoAsync(store, ct));
    }

    /// <summary>查詢登入使用者所屬的商店列表。</summary>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("me")]
    [ProducesResponseType<List<MyStoreDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MyStoreDto>>> GetMineAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var memberships = await db.StoreMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(db.Stores.AsNoTracking(), m => m.StoreId, s => s.Id, (m, s) => new { m.Role, Store = s })
            .ToListAsync(ct);

        var result = new List<MyStoreDto>();
        foreach (var item in memberships)
            result.Add(new MyStoreDto { Store = await ToDtoAsync(item.Store, ct), Role = item.Role });

        return Ok(result);
    }

    /// <summary>更新商店基本資料（StoreName / Description）。僅 Owner 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="request">欲更新的欄位。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<StoreDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreDto>> UpdateAsync(Guid id, [FromBody] UpdateStoreRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("找不到商店。");

        await StoreAuthorization.EnsureOwnerAsync(db, id, userId, ct);

        if (request.StoreName is not null)
        {
            var storeName = request.StoreName.Trim();
            if (storeName.Length is < 1 or > 100)
                throw new ValidationException("商店名稱長度須為 1–100 字。");

            store.StoreName = storeName;
        }

        if (request.Description is not null)
            store.Description = request.Description.Length == 0 ? null : request.Description;

        await db.SaveChangesAsync(ct);

        return Ok(await ToDtoAsync(store, ct));
    }

    /// <summary>平台停權商店（Active → Suspended）。僅 Admin 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/suspend")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SuspendAsync(Guid id, CancellationToken ct)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("找不到商店。");

        if (store.Status != StoreStatus.Active)
            throw new ValidationException("僅 Active 狀態的商店可停權。");

        store.Status = StoreStatus.Suspended;

        AddAuditLogOutbox(currentUser.UserId, "store.suspend", id);

        await db.SaveChangesAsync(ct);

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
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("找不到商店。");

        if (store.Status != StoreStatus.Suspended)
            throw new ValidationException("僅 Suspended 狀態的商店可解除停權。");

        store.Status = StoreStatus.Active;

        AddAuditLogOutbox(currentUser.UserId, "store.unsuspend", id);

        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>關閉商店（Active/Suspended → Closed，終態不可逆）。Owner 或 Admin 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CloseAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("找不到商店。");

        if (!User.IsInRole("Admin"))
            await StoreAuthorization.EnsureOwnerAsync(db, id, userId, ct);

        if (store.Status == StoreStatus.Closed)
            throw new ValidationException("商店已關閉。");

        store.Status = StoreStatus.Closed;

        AddAuditLogOutbox(userId, "store.close", id);

        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>申請商店頭像（Avatar）上傳簽章 URL。僅 Owner 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="request">檔案資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/avatar/upload-url")]
    [ProducesResponseType<AssetUploadUrlResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<AssetUploadUrlResponse>> RequestAvatarUploadUrlAsync(
        Guid id, [FromBody] RequestAssetUploadUrlRequest request, CancellationToken ct) =>
        RequestAssetUploadUrlAsync(id, request, isAvatar: true, ct);

    /// <summary>申請商店橫幅（Banner）上傳簽章 URL。僅 Owner 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="request">檔案資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/banner/upload-url")]
    [ProducesResponseType<AssetUploadUrlResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<AssetUploadUrlResponse>> RequestBannerUploadUrlAsync(
        Guid id, [FromBody] RequestAssetUploadUrlRequest request, CancellationToken ct) =>
        RequestAssetUploadUrlAsync(id, request, isAvatar: false, ct);

    private async Task<ActionResult<AssetUploadUrlResponse>> RequestAssetUploadUrlAsync(
        Guid id, RequestAssetUploadUrlRequest request, bool isAvatar, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("找不到商店。");

        await StoreAuthorization.EnsureOwnerAsync(db, id, userId, ct);

        if (!AllowedImageContentTypes.Contains(request.ContentType))
            throw new ValidationException($"不支援的檔案類型：{request.ContentType}");

        var result = await storageClient.RequestPublicImageUploadUrlAsync(
            userId, request.FileName, request.ContentType, request.SizeBytes, ct);

        var asset = new Asset
        {
            Id = result.FileId,
            CreatedBy = userId,
            StorageKey = result.StorageKey,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.SizeBytes,
        };
        db.Assets.Add(asset);

        if (isAvatar)
            store.AvatarAssetId = asset.Id;
        else
            store.BannerAssetId = asset.Id;

        await db.SaveChangesAsync(ct);

        return Ok(new AssetUploadUrlResponse
        {
            AssetId = asset.Id,
            UploadUrl = result.UploadUrl,
            PublicUrl = result.PublicUrl ?? $"{_publicBaseUrl}/{result.StorageKey}",
            ExpiresAt = result.ExpiresAt,
        });
    }

    private async Task<Store?> FindStoreAsync(string idOrSlug, CancellationToken ct) =>
        Guid.TryParse(idOrSlug, out var id)
            ? await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            : await db.Stores.FirstOrDefaultAsync(s => s.StoreSlug == idOrSlug, ct);

    private async Task<StoreDto> ToDtoAsync(Store store, CancellationToken ct) => new()
    {
        Id = store.Id,
        StoreName = store.StoreName,
        StoreSlug = store.StoreSlug,
        Description = store.Description,
        AvatarUrl = await GetAssetUrlAsync(store.AvatarAssetId, ct),
        BannerUrl = await GetAssetUrlAsync(store.BannerAssetId, ct),
        Status = store.Status,
        CreatedAt = store.CreatedAt,
        UpdatedAt = store.UpdatedAt,
    };

    private async Task<string?> GetAssetUrlAsync(Guid? assetId, CancellationToken ct)
    {
        if (assetId is null)
            return null;

        var storageKey = await db.Assets.AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(a => a.StorageKey)
            .FirstOrDefaultAsync(ct);

        return storageKey is null ? null : $"{_publicBaseUrl}/{storageKey}";
    }

    private void AddAuditLogOutbox(Guid? who, string action, Guid storeId)
    {
        var outbox = new OutboxMessage { EventType = "audit." + action };
        outbox.Payload = JsonSerializer.Serialize(new AuditLogRequestedEvent(
            OutboxMessageId: outbox.Id,
            Who:             who,
            Action:          action,
            Target:          "Store",
            TargetId:        storeId,
            Result:          "success",
            Before:          null,
            After:           null,
            Ip:              HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent:       HttpContext.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null,
            Tenant:          storeId,
            OccurredAt:      DateTimeOffset.UtcNow,
            CorrelationId:   HttpContext.TraceIdentifier));

        db.OutboxMessages.Add(outbox);
    }
}
