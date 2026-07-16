using ContentService.Data.Entities;

namespace ContentService.Models;

/// <summary>法律文件列表查詢請求（分頁採 offset / limit）。</summary>
public class ListLegalDocumentsRequest
{
    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;

    /// <summary>過濾文件類型；null 表示不限。</summary>
    /// <example>TermsOfService</example>
    public LegalDocumentType? Type { get; set; }

    /// <summary>過濾文件狀態；null 表示不限。</summary>
    /// <example>Active</example>
    public LegalDocumentStatus? Status { get; set; }
}

/// <summary>法律文件摘要（列表用，不含完整內容）。</summary>
public class LegalDocumentSummaryDto
{
    /// <summary>文件唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>文件類型。</summary>
    /// <example>TermsOfService</example>
    public LegalDocumentType Type { get; set; }

    /// <summary>版本序號（同類型內遞增）。</summary>
    /// <example>2</example>
    public int Version { get; set; }

    /// <summary>文件標題。</summary>
    /// <example>服務條款</example>
    public string Title { get; set; } = "";

    /// <summary>文件狀態。</summary>
    /// <example>Active</example>
    public LegalDocumentStatus Status { get; set; }

    /// <summary>最近一次啟用時間（UTC）；null 表示從未啟用。</summary>
    /// <example>2026-07-01T00:00:00Z</example>
    public DateTimeOffset? ActivatedAt { get; set; }

    /// <summary>建立時間（UTC）。</summary>
    /// <example>2026-06-30T08:00:00Z</example>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>最後更新時間（UTC）；null 表示自建立後未變更。</summary>
    /// <example>2026-07-01T00:00:00Z</example>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>法律文件完整內容（單筆查詢 / 公開撈取用）。</summary>
public class LegalDocumentDto : LegalDocumentSummaryDto
{
    /// <summary>文件內容（純文字；「## 」開頭為章節標題、「- 」開頭為列點）。</summary>
    /// <example>## 歡迎加入 Open Jam</example>
    public string Content { get; set; } = "";

    /// <summary>重點速覽（純文字；每行一則，以第一個「|」分隔標題與描述）；空字串表示此版本無速覽。</summary>
    /// <example>作品永遠是你的|上架不轉移著作權，你只授權平台展示與銷售你的作品。</example>
    public string Highlights { get; set; } = "";
}

/// <summary>法律文件分頁查詢回應。</summary>
public class ListLegalDocumentsResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>6</example>
    public int TotalCount { get; set; }

    /// <summary>本頁文件清單。</summary>
    public List<LegalDocumentSummaryDto> Items { get; set; } = [];
}

/// <summary>建立法律文件草稿請求；版本序號由伺服器依同類型現有最大版本 +1 產生。</summary>
public class CreateLegalDocumentRequest
{
    /// <summary>文件類型。</summary>
    /// <example>TermsOfService</example>
    public LegalDocumentType Type { get; set; }

    /// <summary>文件標題。</summary>
    /// <example>服務條款</example>
    public string Title { get; set; } = "";

    /// <summary>文件內容（純文字；「## 」開頭為章節標題、「- 」開頭為列點）。</summary>
    /// <example>## 歡迎加入 Open Jam</example>
    public string Content { get; set; } = "";

    /// <summary>重點速覽（純文字；每行一則，以第一個「|」分隔標題與描述）；可留空表示無速覽。</summary>
    /// <example>作品永遠是你的|上架不轉移著作權，你只授權平台展示與銷售你的作品。</example>
    public string Highlights { get; set; } = "";
}

/// <summary>更新法律文件草稿請求；僅 Draft 狀態可更新。</summary>
public class UpdateLegalDocumentRequest
{
    /// <summary>文件標題。</summary>
    /// <example>服務條款</example>
    public string Title { get; set; } = "";

    /// <summary>文件內容（純文字；「## 」開頭為章節標題、「- 」開頭為列點）。</summary>
    /// <example>## 歡迎加入 Open Jam</example>
    public string Content { get; set; } = "";

    /// <summary>重點速覽（純文字；每行一則，以第一個「|」分隔標題與描述）；可留空表示無速覽。</summary>
    /// <example>作品永遠是你的|上架不轉移著作權，你只授權平台展示與銷售你的作品。</example>
    public string Highlights { get; set; } = "";
}
