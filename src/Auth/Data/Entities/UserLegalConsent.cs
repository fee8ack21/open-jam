using Shared.Audit;

namespace Auth.Data.Entities;

/// <summary>
/// 使用者對特定法律文件版本的同意紀錄（append-only）。
/// 一位使用者對同一文件版本僅一筆；同意時間即 CreatedAt。
/// 文件本身由 ContentService 管理，<see cref="LegalDocumentId"/> 為跨服務參照（無外鍵）。
/// </summary>
public class UserLegalConsent : ICreatedAt
{
    /// <summary>同意紀錄唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>同意者的使用者 ID。</summary>
    public Guid UserId { get; set; }

    /// <summary>被同意的法律文件版本 ID（ContentService 的 LegalDocument.Id，跨服務參照）。</summary>
    public Guid LegalDocumentId { get; set; }

    /// <summary>同意時間（UTC），由 BaseDbContext 於新增時自動填入。</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>關聯的使用者。</summary>
    public User User { get; set; } = null!;
}
