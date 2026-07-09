using Shared.Audit;

namespace ContentService.Data.Entities;

/// <summary>
/// 法律文件（服務條款 / 隱私權政策）版本。
/// 每次修訂為一筆新紀錄（同類型 Version 遞增），永不刪除，供同意紀錄與歷史比對。
/// 僅 Draft 可編輯；啟用時同類型既有 Active 文件自動轉為 Inactive。
/// </summary>
public class LegalDocument : ICreatedAt, IUpdatedAt, IUpdatedBy
{
    /// <summary>文件唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>文件類型。</summary>
    public LegalDocumentType Type { get; set; }

    /// <summary>版本序號，同一類型內遞增且唯一。</summary>
    public int Version { get; set; }

    /// <summary>文件標題。</summary>
    public string Title { get; set; } = "";

    /// <summary>文件內容（純文字；「## 」開頭為章節標題、「- 」開頭為列點）。</summary>
    public string Content { get; set; } = "";

    /// <summary>文件狀態。</summary>
    public LegalDocumentStatus Status { get; set; } = LegalDocumentStatus.Draft;

    /// <summary>最近一次啟用時間（UTC）；null 表示從未啟用。</summary>
    public DateTimeOffset? ActivatedAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <inheritdoc/>
    public Guid? UpdatedBy { get; private set; }
}
