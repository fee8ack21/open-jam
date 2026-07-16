using AutoMapper;
using AutoMapper.QueryableExtensions;
using ContentService.Data;
using ContentService.Data.Entities;
using ContentService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;
using Shared.Exceptions;

namespace ContentService.Services.Legal;

/// <summary>法律文件版本管理的具體實作。啟用過的版本不可刪除，僅以狀態控制生效與否；草稿可軟刪除。</summary>
public class LegalDocumentService(
    ContentDbContext db,
    IMapper mapper,
    AuditLogPublisher audit,
    ICurrentUserAccessor currentUser) : ILegalDocumentService
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
        // 含已軟刪除的草稿一併計算，避免新版本號撞到 (Type, Version) 唯一索引
        var maxVersion = await db.LegalDocuments
            .IgnoreQueryFilters()
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
        audit.Add(currentUser.UserId, "legal.create", "LegalDocument", doc.Id);
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
        audit.Add(currentUser.UserId, "legal.update", "LegalDocument", doc.Id);
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
            audit.Add(currentUser.UserId, "legal.activate", "LegalDocument", doc.Id);
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
        audit.Add(currentUser.UserId, "legal.deactivate", "LegalDocument", doc.Id);
        await db.SaveChangesAsync(ct);

        return mapper.Map<LegalDocumentDto>(doc);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await db.LegalDocuments.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的法律文件");

        if (doc.Status != LegalDocumentStatus.Draft)
            throw new ConflictException("僅草稿狀態的文件可刪除；已啟用或停用的版本為歷史紀錄，不可刪除");

        db.LegalDocuments.Remove(doc);
        audit.Add(currentUser.UserId, "legal.delete", "LegalDocument", doc.Id);
        await db.SaveChangesAsync(ct);
    }
}
