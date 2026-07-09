using Auth.Data.Entities;

namespace Auth.Models;

/// <summary>
/// 啟用中法律文件的檢視模型；由 ContentService 的公開端點
/// <c>GET /v1/legal-documents/active</c> 取回並反序列化，供註冊 / re-consent 畫面渲染
/// 與同意紀錄比對。僅保留 Auth 所需欄位，ContentService 回傳的其餘欄位忽略。
/// </summary>
public class LegalDocumentDto
{
    /// <summary>文件唯一識別碼（作為同意紀錄的 LegalDocumentId）。</summary>
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

    /// <summary>文件內容（純文字；「## 」開頭為章節標題、「- 」開頭為列點）。</summary>
    /// <example>## 歡迎加入 Open Jam</example>
    public string Content { get; set; } = "";

    /// <summary>最近一次啟用時間（UTC）；null 表示從未啟用。</summary>
    /// <example>2026-07-01T00:00:00Z</example>
    public DateTimeOffset? ActivatedAt { get; set; }

    /// <summary>建立時間（UTC）；ActivatedAt 為 null 時作為畫面「最後更新」的備援顯示。</summary>
    /// <example>2026-06-30T08:00:00Z</example>
    public DateTimeOffset CreatedAt { get; set; }
}
