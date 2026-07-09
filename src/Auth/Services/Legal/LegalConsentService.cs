using Auth.Data;
using Auth.Data.Entities;
using Auth.Models;
using Auth.Services.Content;
using Microsoft.EntityFrameworkCore;

namespace Auth.Services.Legal;

/// <summary>
/// 使用者法律文件同意的具體實作。
/// 啟用中文件即時向 ContentService 取得，未同意清單以本地同意紀錄比對計算。
/// </summary>
public class LegalConsentService(AppDbContext db, ContentServiceClient contentClient) : ILegalConsentService
{
    /// <inheritdoc/>
    public Task<List<LegalDocumentDto>> GetActiveAsync(LegalDocumentType? type = null, CancellationToken ct = default)
        => contentClient.GetActiveAsync(type, ct);

    /// <inheritdoc/>
    public async Task<List<LegalDocumentDto>> GetPendingConsentAsync(Guid userId, CancellationToken ct = default)
    {
        var active = await contentClient.GetActiveAsync(null, ct);
        if (active.Count == 0) return [];

        var activeIds = active.Select(d => d.Id).ToList();
        var consented = await db.UserLegalConsents
            .Where(c => c.UserId == userId && activeIds.Contains(c.LegalDocumentId))
            .Select(c => c.LegalDocumentId)
            .ToListAsync(ct);

        return active
            .Where(d => !consented.Contains(d.Id))
            .OrderBy(d => d.Type)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task RecordConsentsAsync(Guid userId, IReadOnlyCollection<Guid> documentIds, CancellationToken ct = default)
    {
        if (documentIds.Count == 0) return;

        var existing = await db.UserLegalConsents
            .Where(c => c.UserId == userId && documentIds.Contains(c.LegalDocumentId))
            .Select(c => c.LegalDocumentId)
            .ToListAsync(ct);

        var missing = documentIds.Except(existing).ToList();
        if (missing.Count == 0) return;

        foreach (var docId in missing)
            db.UserLegalConsents.Add(new UserLegalConsent { UserId = userId, LegalDocumentId = docId });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // 並發重複同意（重複送出）視為已成功，同意紀錄以 (UserId, LegalDocumentId) 唯一
        }
    }
}
