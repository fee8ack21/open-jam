using Shared.Audit;

namespace ContentService.Data.Entities;

/// <summary>
/// 常見問題（FAQ）項目。依主題分類（<see cref="FaqCategory"/>）分組，
/// 於同分類內以 <see cref="SortOrder"/> 排序；僅 <see cref="IsPublished"/> 為 true 者對外公開。
/// </summary>
public class FaqItem : ICreatedAt, IUpdatedAt, IUpdatedBy
{
    /// <summary>項目唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>主題分類。</summary>
    public FaqCategory Category { get; set; }

    /// <summary>問題。</summary>
    public string Question { get; set; } = "";

    /// <summary>解答（純文字，可含換行）。</summary>
    public string Answer { get; set; } = "";

    /// <summary>同分類內的顯示排序（升冪）。</summary>
    public int SortOrder { get; set; }

    /// <summary>是否已發布（對外公開）。</summary>
    public bool IsPublished { get; set; } = true;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <inheritdoc/>
    public Guid? UpdatedBy { get; private set; }
}
