using Auth.Data.Entities;
using Auth.Models;

namespace Auth.Services.Legal;

/// <summary>
/// 使用者法律文件同意（consent）的業務邏輯。
/// 文件本身由 ContentService 管理（透過 <see cref="Content.ContentServiceClient"/> 即時取得），
/// 同意紀錄（<see cref="UserLegalConsent"/>）則留在 Auth 本地資料庫。
/// </summary>
public interface ILegalConsentService
{
    /// <summary>取得目前啟用中的法律文件（呼叫 ContentService）；type 為 null 時回傳所有類型。</summary>
    Task<List<LegalDocumentDto>> GetActiveAsync(LegalDocumentType? type = null, CancellationToken ct = default);

    /// <summary>取得使用者尚未同意的啟用中文件（登入 re-consent 檢查用）。</summary>
    Task<List<LegalDocumentDto>> GetPendingConsentAsync(Guid userId, CancellationToken ct = default);

    /// <summary>寫入使用者對指定文件版本的同意紀錄；已存在者略過（冪等）。</summary>
    Task RecordConsentsAsync(Guid userId, IReadOnlyCollection<Guid> documentIds, CancellationToken ct = default);
}
