using System.Net.Http.Json;

namespace CatalogService.Services;

/// <summary>呼叫 StorageService 簽發上傳 URL 的客戶端。</summary>
public class StorageServiceClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>申請上傳簽章 URL。</summary>
    /// <param name="creatorId">擁有者（商店 Owner）使用者 ID。</param>
    /// <param name="catalogId">所屬商品 ID。</param>
    /// <param name="fileName">原始檔名。</param>
    /// <param name="contentType">MIME 類型。</param>
    /// <param name="sizeBytes">檔案大小（bytes）。</param>
    /// <param name="fileType">StorageService 媒體分類（Video=0, Image=1, Pdf=2）。</param>
    /// <param name="isPublic">是否為公開讀取物件（展示型資產為 true，下載檔為 false）。</param>
    /// <param name="reservationId">配額預扣紀錄 ID；隨 FileReadyEvent 回帶供 QuotaService commit。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<StorageUploadUrlResult> RequestUploadUrlAsync(
        Guid creatorId, Guid catalogId, string fileName, string contentType, long sizeBytes,
        StorageFileType fileType, bool isPublic, Guid reservationId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("storage");

        var response = await client.PostAsJsonAsync("v1/files/upload-url", new
        {
            CreatorId = creatorId,
            ProductId = (Guid?)catalogId,
            ReservationId = (Guid?)reservationId,
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

/// <summary>對應 StorageService <c>FileType</c> enum（Video=0, Image=1, Pdf=2）。</summary>
public enum StorageFileType
{
    /// <summary>影片。</summary>
    Video = 0,

    /// <summary>圖片。</summary>
    Image = 1,

    /// <summary>PDF 文件。</summary>
    Pdf = 2,
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
