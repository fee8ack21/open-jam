using Auth.Data.Entities;
using Auth.Models;

namespace Auth.Services.Legal;

/// <summary>法律文件（服務條款 / 隱私權政策）版本管理與使用者同意紀錄的業務邏輯。</summary>
public interface ILegalDocumentService
{
    /// <summary>分頁查詢法律文件（管理員後台列表）。</summary>
    Task<ListLegalDocumentsResponse> ListAsync(ListLegalDocumentsRequest request, CancellationToken ct = default);

    /// <summary>取得單筆法律文件完整內容。</summary>
    Task<LegalDocumentDto> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>取得目前啟用中的法律文件；type 為 null 時回傳所有類型的啟用版本。</summary>
    Task<List<LegalDocumentDto>> GetActiveAsync(LegalDocumentType? type, CancellationToken ct = default);

    /// <summary>建立草稿，版本序號取同類型現有最大版本 +1。</summary>
    Task<LegalDocumentDto> CreateAsync(CreateLegalDocumentRequest request, CancellationToken ct = default);

    /// <summary>更新草稿標題與內容；非 Draft 狀態擲出 Conflict。</summary>
    Task<LegalDocumentDto> UpdateAsync(Guid id, UpdateLegalDocumentRequest request, CancellationToken ct = default);

    /// <summary>啟用文件（Draft / Inactive → Active），同類型既有啟用版本自動轉為 Inactive。</summary>
    Task<LegalDocumentDto> ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>停用啟用中的文件（Active → Inactive）；非 Active 狀態擲出 Conflict。</summary>
    Task<LegalDocumentDto> DeactivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>取得使用者尚未同意的啟用中文件（登入 re-consent 檢查用）。</summary>
    Task<List<LegalDocumentDto>> GetPendingConsentAsync(Guid userId, CancellationToken ct = default);

    /// <summary>寫入使用者對指定文件版本的同意紀錄；已存在者略過（冪等）。</summary>
    Task RecordConsentsAsync(Guid userId, IReadOnlyCollection<Guid> documentIds, CancellationToken ct = default);
}
