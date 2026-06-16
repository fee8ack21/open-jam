namespace CatalogService.Models;

/// <summary>商品版本回應。</summary>
public class CatalogVersionDto
{
    /// <summary>版本唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>所屬商品 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CatalogId { get; set; }

    /// <summary>版本字串。</summary>
    /// <example>1.0.0</example>
    public string Version { get; set; } = "";

    /// <summary>版本說明 / 更新紀錄。</summary>
    /// <example>首次發行。</example>
    public string? ReleaseNote { get; set; }

    /// <summary>本版本的可下載檔案清單。</summary>
    public List<CatalogVersionAssetDto> Assets { get; set; } = [];

    /// <summary>建立時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>商品版本可下載檔案回應。</summary>
public class CatalogVersionAssetDto
{
    /// <summary>資產唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>原始檔名。</summary>
    /// <example>pixel-sfx-pack-v1.zip</example>
    public string FileName { get; set; } = "";

    /// <summary>MIME 類型。</summary>
    /// <example>application/pdf</example>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）。</summary>
    /// <example>10485760</example>
    public long FileSize { get; set; }

    /// <summary>同版本內顯示排序。</summary>
    /// <example>0</example>
    public int SortOrder { get; set; }

    /// <summary>建立時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>建立商品版本請求。</summary>
public class CreateCatalogVersionRequest
{
    /// <summary>版本字串（同商品內唯一，1–50 字）。</summary>
    /// <example>1.0.0</example>
    public string Version { get; set; } = "";

    /// <summary>版本說明 / 更新紀錄。</summary>
    /// <example>首次發行。</example>
    public string? ReleaseNote { get; set; }
}

/// <summary>申請版本可下載檔案上傳簽章 URL 請求。</summary>
public class RequestVersionAssetUploadUrlRequest
{
    /// <summary>原始檔名（含副檔名）。</summary>
    /// <example>pixel-sfx-pack-v1.pdf</example>
    public string FileName { get; set; } = "";

    /// <summary>MIME 類型。</summary>
    /// <example>application/pdf</example>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）。</summary>
    /// <example>10485760</example>
    public long SizeBytes { get; set; }
}

/// <summary>版本可下載檔案上傳簽章 URL 回應（私有物件，無公開讀取網址）。</summary>
public class VersionAssetUploadUrlResponse
{
    /// <summary>已建立的 Asset ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid AssetId { get; set; }

    /// <summary>前端應使用此 URL 以 HTTP PUT 直傳檔案。</summary>
    /// <example>http://localhost:5171/v1/files/blob/creators/.../pixel-sfx-pack-v1.pdf?expires=1735689600&amp;sig=...</example>
    public string UploadUrl { get; set; } = "";

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
