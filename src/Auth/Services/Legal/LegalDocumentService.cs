using AutoMapper;
using AutoMapper.QueryableExtensions;
using Auth.Data;
using Auth.Data.Entities;
using Auth.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Exceptions;

namespace Auth.Services.Legal;

/// <summary>法律文件版本管理與使用者同意紀錄的具體實作。文件永不刪除，僅以狀態控制生效與否。</summary>
public class LegalDocumentService(AppDbContext db, IMapper mapper) : ILegalDocumentService
{
    /// <inheritdoc/>
    public async Task<ListLegalDocumentsResponse> ListAsync(ListLegalDocumentsRequest request, CancellationToken ct = default)
    {
        var query = db.LegalDocuments.AsNoTracking();

        if (request.Type.HasValue)
            query = query.Where(d => d.Type == request.Type);

        if (request.Status.HasValue)
            query = query.Where(d => d.Status == request.Status);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(d => d.Type)
            .ThenByDescending(d => d.Version)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ProjectTo<LegalDocumentSummaryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new ListLegalDocumentsResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await db.LegalDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的法律文件");
        return mapper.Map<LegalDocumentDto>(doc);
    }

    /// <inheritdoc/>
    public async Task<List<LegalDocumentDto>> GetActiveAsync(LegalDocumentType? type, CancellationToken ct = default)
    {
        var query = db.LegalDocuments.AsNoTracking().Where(d => d.Status == LegalDocumentStatus.Active);

        if (type.HasValue)
            query = query.Where(d => d.Type == type);

        return await query
            .OrderBy(d => d.Type)
            .ProjectTo<LegalDocumentDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentDto> CreateAsync(CreateLegalDocumentRequest request, CancellationToken ct = default)
    {
        var maxVersion = await db.LegalDocuments
            .Where(d => d.Type == request.Type)
            .MaxAsync(d => (int?)d.Version, ct) ?? 0;

        var doc = new LegalDocument
        {
            Type    = request.Type,
            Version = maxVersion + 1,
            Title   = request.Title.Trim(),
            Content = request.Content,
            Status  = LegalDocumentStatus.Draft,
        };

        db.LegalDocuments.Add(doc);
        await db.SaveChangesAsync(ct);

        return mapper.Map<LegalDocumentDto>(doc);
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentDto> UpdateAsync(Guid id, UpdateLegalDocumentRequest request, CancellationToken ct = default)
    {
        var doc = await db.LegalDocuments.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的法律文件");

        if (doc.Status != LegalDocumentStatus.Draft)
            throw new ConflictException("僅草稿狀態的文件可編輯；已啟用或停用的版本為歷史紀錄，不可修改");

        doc.Title   = request.Title.Trim();
        doc.Content = request.Content;
        await db.SaveChangesAsync(ct);

        return mapper.Map<LegalDocumentDto>(doc);
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentDto> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Database.ExecuteInTransactionAsync(async tx =>
        {
            var doc = await db.LegalDocuments.FirstOrDefaultAsync(d => d.Id == id, ct)
                ?? throw new NotFoundException("找不到指定的法律文件");

            if (doc.Status == LegalDocumentStatus.Active)
                return mapper.Map<LegalDocumentDto>(doc);

            // 先讓既有啟用版本失效並落盤，再啟用新版本，避免違反「同類型僅一筆 Active」的 partial unique index
            var currents = await db.LegalDocuments
                .Where(d => d.Type == doc.Type && d.Status == LegalDocumentStatus.Active)
                .ToListAsync(ct);

            foreach (var current in currents)
                current.Status = LegalDocumentStatus.Inactive;

            if (currents.Count > 0)
                await db.SaveChangesAsync(ct);

            doc.Status      = LegalDocumentStatus.Active;
            doc.ActivatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return mapper.Map<LegalDocumentDto>(doc);
        }, ct);
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentDto> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await db.LegalDocuments.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的法律文件");

        if (doc.Status != LegalDocumentStatus.Active)
            throw new ConflictException("僅啟用中的文件可停用");

        doc.Status = LegalDocumentStatus.Inactive;
        await db.SaveChangesAsync(ct);

        return mapper.Map<LegalDocumentDto>(doc);
    }

    /// <inheritdoc/>
    public async Task<List<LegalDocumentDto>> GetPendingConsentAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.LegalDocuments.AsNoTracking()
            .Where(d => d.Status == LegalDocumentStatus.Active)
            .Where(d => !db.UserLegalConsents.Any(c => c.UserId == userId && c.LegalDocumentId == d.Id))
            .OrderBy(d => d.Type)
            .ProjectTo<LegalDocumentDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
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
