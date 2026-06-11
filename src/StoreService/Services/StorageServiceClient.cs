using System.Net.Http.Json;

namespace StoreService.Services;

/// <summary>呼叫 StorageService 簽發上傳 URL 的客戶端。</summary>
public class StorageServiceClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>申請公開讀取物件（Avatar/Banner）的上傳簽章 URL。</summary>
    /// <param name="creatorId">擁有者（商店 Owner）使用者 ID。</param>
    /// <param name="fileName">原始檔名。</param>
    /// <param name="contentType">MIME 類型。</param>
    /// <param name="sizeBytes">檔案大小（bytes）。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<StorageUploadUrlResult> RequestPublicImageUploadUrlAsync(
        Guid creatorId, string fileName, string contentType, long sizeBytes, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("storage");

        var response = await client.PostAsJsonAsync("files/upload-url", new
        {
            CreatorId = creatorId,
            ProductId = (Guid?)null,
            OriginalName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            FileType = StorageFileType.Image,
            IsPreview = false,
            IsPublic = true,
        }, ct);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<StorageUploadUrlResult>(ct))!;
    }

    /// <summary>對應 StorageService <c>FileType</c> enum（Video=0, Image=1, Pdf=2）。</summary>
    private enum StorageFileType
    {
        Video = 0,
        Image = 1,
        Pdf = 2,
    }
}

/// <summary>StorageService <c>POST /files/upload-url</c> 回應。</summary>
public class StorageUploadUrlResult
{
    /// <summary>已建立的檔案紀錄 ID。</summary>
    public Guid FileId { get; set; }

    /// <summary>前端應使用此 URL 以 HTTP PUT 直傳檔案。</summary>
    public string UploadUrl { get; set; } = "";

    /// <summary>在儲存後端的物件鍵值。</summary>
    public string StorageKey { get; set; } = "";

    /// <summary>公開讀取網址。</summary>
    public string? PublicUrl { get; set; }

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
