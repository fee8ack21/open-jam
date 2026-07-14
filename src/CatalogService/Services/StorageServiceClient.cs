using System.Net;
using System.Net.Http.Json;
using Shared.Exceptions;

namespace CatalogService.Services;

/// <summary>呼叫 StorageService 簽發上傳 URL 與管理檔案生命週期的客戶端。</summary>
public class StorageServiceClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>申請上傳簽章 URL。簽發階段不涉及配額，配額於使用者提交確認時扣量。</summary>
    /// <param name="creatorId">擁有者（商店 Owner）使用者 ID。</param>
    /// <param name="catalogId">所屬商品 ID。</param>
    /// <param name="fileName">原始檔名。</param>
    /// <param name="contentType">MIME 類型。</param>
    /// <param name="sizeBytes">檔案大小（bytes）。</param>
    /// <param name="fileType">StorageService 媒體分類（Video=0, Image=1, Pdf=2）。</param>
    /// <param name="isPublic">是否為公開讀取物件（展示型資產為 true，下載檔為 false）。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<StorageUploadUrlResult> RequestUploadUrlAsync(
        Guid creatorId, Guid catalogId, string fileName, string contentType, long sizeBytes,
        StorageFileType fileType, bool isPublic, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("storage");

        var response = await client.PostAsJsonAsync("v1/files/upload-url", new
        {
            CreatorId = creatorId,
            ProductId = (Guid?)catalogId,
            OriginalName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            FileType = fileType,
            IsPreview = isPublic,
            IsPublic = isPublic,
        }, ct);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<StorageUploadUrlResult>(ct))!;
    }

    /// <summary>確認檔案已直傳完成，觸發處理 pipeline 並回傳最新檔案狀態（含實際大小）。冪等。</summary>
    /// <param name="fileId">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<StorageFileResult> ConfirmUploadAsync(Guid fileId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("storage");

        var response = await client.PostAsync($"v1/files/{fileId}/confirm", content: null, ct);
        EnsureFileSuccess(response, "檔案尚未上傳完成。");

        return (await response.Content.ReadFromJsonAsync<StorageFileResult>(ct))!;
    }

    /// <summary>標記檔案已被實際使用（建立 File reference 後呼叫）。冪等。</summary>
    /// <param name="fileId">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task MarkReferencedAsync(Guid fileId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("storage");

        var response = await client.PostAsync($"v1/files/{fileId}/reference", content: null, ct);
        EnsureFileSuccess(response, "檔案尚未完成處理，無法標記為已使用。");
    }

    /// <summary>軟刪除檔案（資產移除時呼叫，讓配額對帳釋放用量）。檔案不存在視為已刪除。</summary>
    /// <param name="fileId">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task DeleteFileAsync(Guid fileId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("storage");

        var response = await client.DeleteAsync($"v1/files/{fileId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return; // 冪等：已刪除或不存在皆視為成功

        response.EnsureSuccessStatusCode();
    }

    /// <summary>將 StorageService 的檔案狀態錯誤轉為對應 AppException，維持給前端的契約。</summary>
    private static void EnsureFileSuccess(HttpResponseMessage response, string unprocessableDetail)
    {
        if (response.IsSuccessStatusCode)
            return;

        throw response.StatusCode switch
        {
            HttpStatusCode.NotFound => new NotFoundException("找不到檔案。"),
            HttpStatusCode.UnprocessableEntity => new ValidationException(unprocessableDetail),
            _ => new Exception($"StorageService 回應非預期狀態：{(int)response.StatusCode}"),
        };
    }

    /// <summary>取得已授權的下載簽章 URL（買家 entitlement 由呼叫端先行驗證）。</summary>
    /// <param name="fileId">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<StorageDownloadUrlResult> GetDownloadUrlAsync(Guid fileId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("storage");

        var response = await client.GetAsync($"v1/files/{fileId}/download-url", ct);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<StorageDownloadUrlResult>(ct))!;
    }
}

/// <summary>對應 StorageService <c>FileType</c> enum（Video=0, Image=1, Pdf=2, Other=3）。</summary>
public enum StorageFileType
{
    /// <summary>影片。</summary>
    Video = 0,

    /// <summary>圖片。</summary>
    Image = 1,

    /// <summary>PDF 文件。</summary>
    Pdf = 2,

    /// <summary>其他二進位下載檔（ZIP / 音訊 / 設計檔等），不做處理。</summary>
    Other = 3,
}

/// <summary>StorageService <c>POST /v1/files/upload-url</c> 回應。</summary>
public class StorageUploadUrlResult
{
    /// <summary>已建立的檔案紀錄 ID。</summary>
    public Guid FileId { get; set; }

    /// <summary>前端應使用此 URL 以 HTTP PUT 直傳檔案。</summary>
    public string UploadUrl { get; set; } = "";

    /// <summary>在儲存後端的物件鍵值。</summary>
    public string StorageKey { get; set; } = "";

    /// <summary>公開讀取網址；僅公開物件時提供。</summary>
    public string? PublicUrl { get; set; }

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>對應 StorageService <c>FileStatus</c> enum（Uploading=0, Processing=1, Ready=2, Failed=3）。</summary>
public enum StorageFileStatus
{
    /// <summary>已簽發上傳 URL，等待直傳完成。</summary>
    Uploading = 0,

    /// <summary>正在進行掃毒 / 轉碼 / 預覽生成。</summary>
    Processing = 1,

    /// <summary>所有處理完成，可對外提供。</summary>
    Ready = 2,

    /// <summary>處理失敗或上傳逾時。</summary>
    Failed = 3,
}

/// <summary>StorageService 檔案紀錄回應（<c>POST /v1/files/{id}/confirm</c> 等）。</summary>
public class StorageFileResult
{
    /// <summary>檔案 ID。</summary>
    public Guid Id { get; set; }

    /// <summary>擁有者（創作者）ID。</summary>
    public Guid CreatorId { get; set; }

    /// <summary>所屬商品 ID。</summary>
    public Guid? ProductId { get; set; }

    /// <summary>在儲存後端的物件鍵值。</summary>
    public string StorageKey { get; set; } = "";

    /// <summary>使用者上傳時的原始檔名。</summary>
    public string OriginalName { get; set; } = "";

    /// <summary>MIME 類型。</summary>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）；上傳完成前為 null。</summary>
    public long? SizeBytes { get; set; }

    /// <summary>目前處理狀態。</summary>
    public StorageFileStatus Status { get; set; }

    /// <summary>功能 API 確認此檔已被實際使用的時間；null 表示尚未被使用。</summary>
    public DateTimeOffset? ReferencedAt { get; set; }
}

/// <summary>StorageService <c>GET /v1/files/{id}/download-url</c> 回應。</summary>
public class StorageDownloadUrlResult
{
    /// <summary>檔案 ID。</summary>
    public Guid FileId { get; set; }

    /// <summary>短效下載 URL。</summary>
    public string DownloadUrl { get; set; } = "";

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
