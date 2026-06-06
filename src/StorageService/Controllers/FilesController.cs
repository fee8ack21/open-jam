using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Exceptions;
using StorageService.Data;
using StorageService.Data.Entities;
using StorageService.Models;
using StorageService.Options;
using StorageService.Services;
using StorageService.Storage;

namespace StorageService.Controllers;

/// <summary>
/// 數位商品檔案管理 API。
/// 調用方為功能 API（商品服務），前端不直接呼叫此 Controller，
/// 而是透過 presigned URL 直接與 MinIO / GCS 互動。
///
/// 授權說明（MVP 暫無 JWT 驗證，待功能 API 整合後補上 service token 驗證）。
/// </summary>
[ApiController]
[Route("files")]
public class FilesController(
    StorageDbContext db,
    IStorageProvider storage,
    IOptions<StorageOptions> storageOptions) : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "video/mp4", "video/quicktime", "video/x-msvideo", "video/webm",
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
    ];

    /// <summary>申請上傳簽章 URL（presigned PUT）。</summary>
    /// <remarks>
    /// 功能 API 在完成配額檢查後呼叫此端點，取得 presigned URL 交由前端直傳 MinIO / GCS。
    /// 上傳完成後由 Storage bucket notification webhook 自動觸發處理 pipeline，
    /// 無需客戶端另行回報。
    /// </remarks>
    /// <param name="request">上傳元資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>包含 presigned URL 與 fileId 的回應。</returns>
    [HttpPost("upload-url")]
    [ProducesResponseType<RequestUploadUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RequestUploadUrlResponse>> RequestUploadUrlAsync(
        [FromBody] RequestUploadUrlRequest request, CancellationToken ct)
    {
        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new ValidationException($"不支援的檔案類型：{request.ContentType}");

        var opts    = storageOptions.Value;
        var expiry  = TimeSpan.FromSeconds(opts.UploadUrlExpirySeconds);
        var expiresAt = DateTimeOffset.UtcNow.Add(expiry);

        var file = new StoredFile
        {
            CreatorId   = request.CreatorId,
            ProductId   = request.ProductId,
            OriginalName = request.OriginalName,
            ContentType = request.ContentType,
            SizeBytes   = request.SizeBytes,
            FileType    = request.FileType,
            IsPreview   = request.IsPreview,
        };
        file.StorageKey = $"creators/{request.CreatorId}/{file.Id}/{request.OriginalName}";

        db.StoredFiles.Add(file);
        await db.SaveChangesAsync(ct);

        var uploadUrl = await storage.GenerateUploadUrlAsync(
            file.StorageKey, request.ContentType, request.SizeBytes, expiry, ct);

        return Ok(new RequestUploadUrlResponse
        {
            FileId    = file.Id,
            UploadUrl = uploadUrl,
            ExpiresAt = expiresAt,
        });
    }

    /// <summary>查詢檔案元資訊與處理狀態。</summary>
    /// <param name="id">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<FileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var file = await db.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct)
            ?? throw new NotFoundException($"檔案 {id} 不存在");

        return Ok(ToDto(file));
    }

    /// <summary>取得已授權的下載簽章 URL（presigned GET）。</summary>
    /// <remarks>
    /// 此端點不自行驗證買家是否擁有商品（entitlement check），
    /// 應由功能 API 完成授權驗證後再呼叫此端點。
    /// 僅 Ready 狀態的檔案才能取得下載 URL。
    /// </remarks>
    /// <param name="id">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{id:guid}/download-url")]
    [ProducesResponseType<GetDownloadUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<GetDownloadUrlResponse>> GetDownloadUrlAsync(Guid id, CancellationToken ct)
    {
        var file = await db.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct)
            ?? throw new NotFoundException($"檔案 {id} 不存在");

        if (file.Status != FileStatus.Ready)
            throw new ValidationException($"檔案 {id} 尚未完成處理（狀態：{file.Status}）");

        var opts   = storageOptions.Value;
        var expiry = TimeSpan.FromSeconds(opts.DownloadUrlExpirySeconds);

        var downloadUrl = await storage.GenerateDownloadUrlAsync(file.StorageKey, expiry, ct);

        return Ok(new GetDownloadUrlResponse
        {
            FileId      = file.Id,
            DownloadUrl = downloadUrl,
            ExpiresAt   = DateTimeOffset.UtcNow.Add(expiry),
        });
    }

    /// <summary>軟刪除檔案；已購買的商品仍保留買家下載權。</summary>
    /// <param name="id">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var file = await db.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct)
            ?? throw new NotFoundException($"檔案 {id} 不存在");

        file.SoftDelete();
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static FileDto ToDto(StoredFile f) => new()
    {
        Id          = f.Id,
        CreatorId   = f.CreatorId,
        ProductId   = f.ProductId,
        OriginalName = f.OriginalName,
        ContentType = f.ContentType,
        SizeBytes   = f.SizeBytes,
        FileType    = f.FileType,
        Status      = f.Status,
        IsPreview   = f.IsPreview,
        CreatedAt   = f.CreatedAt,
        UpdatedAt   = f.UpdatedAt,
    };
}
