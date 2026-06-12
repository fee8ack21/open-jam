using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Events;
using Shared.Exceptions;
using StoreService.Data;
using StoreService.Data.Entities;
using StoreService.Models;

namespace StoreService.Services.StoreApplications;

/// <summary>開店申請業務邏輯實作。</summary>
public class StoreApplicationService(
    StoreDbContext db,
    ICurrentUserAccessor currentUser,
    AuditLogPublisher auditLog,
    IHttpContextAccessor httpContextAccessor) : IStoreApplicationService
{
    /// <inheritdoc/>
    public async Task<StoreApplicationDto> SubmitAsync(SubmitStoreApplicationRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var email = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new UnauthorizedException();

        var storeName = request.StoreName.Trim();
        if (storeName.Length is < 1 or > 100)
            throw new ValidationException("商店名稱長度須為 1–100 字。");

        var storeSlug = request.StoreSlug.Trim().ToLowerInvariant();
        StoreSlugValidator.ValidateFormat(storeSlug);

        var hasPending = await db.StoreApplications
            .AnyAsync(a => a.UserId == userId && a.Status == StoreApplicationStatus.Pending, ct);
        if (hasPending)
            throw new ValidationException("您已有一筆待審核的開店申請。");

        await StoreSlugValidator.EnsureUniqueAsync(db, storeSlug, ct);

        var application = new StoreApplication
        {
            UserId = userId,
            Email = email,
            StoreName = storeName,
            StoreSlug = storeSlug,
        };
        db.StoreApplications.Add(application);

        auditLog.Add(
            who: userId,
            action: "store.application.submit",
            target: "StoreApplication",
            targetId: application.Id,
            tenant: null);

        await db.SaveChangesAsync(ct);

        return ToDto(application);
    }

    /// <inheritdoc/>
    public async Task<GetStoreApplicationsResponse> GetMineAsync(GetStoreApplicationsRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = db.StoreApplications.AsNoTracking().Where(a => a.UserId == userId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(request.Offset)
            .Take(limit)
            .Select(a => ToDto(a))
            .ToListAsync(ct);

        return new GetStoreApplicationsResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task WithdrawAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var application = await db.StoreApplications.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("找不到開店申請。");

        if (application.UserId != userId)
            throw new ForbiddenException();

        if (application.Status != StoreApplicationStatus.Pending)
            throw new ValidationException("僅待審核的申請可撤回。");

        application.Status = StoreApplicationStatus.Withdrawn;

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<GetStoreApplicationsResponse> GetAllAsync(GetStoreApplicationsRequest request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = db.StoreApplications.AsNoTracking().AsQueryable();
        if (request.Status.HasValue)
            query = query.Where(a => a.Status == request.Status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(request.Offset)
            .Take(limit)
            .Select(a => ToDto(a))
            .ToListAsync(ct);

        return new GetStoreApplicationsResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task<StoreApplicationDto> ApproveAsync(Guid id, CancellationToken ct)
    {
        var adminId = currentUser.UserId ?? throw new UnauthorizedException();

        var application = await db.StoreApplications.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("找不到開店申請。");

        if (application.Status != StoreApplicationStatus.Pending)
            throw new ValidationException("僅待審核的申請可核准。");

        var store = new Store
        {
            StoreName = application.StoreName,
            StoreSlug = application.StoreSlug,
            Status = StoreStatus.Active,
        };
        db.Stores.Add(store);

        db.StoreMembers.Add(new StoreMember
        {
            StoreId = store.Id,
            UserId = application.UserId,
            Role = StoreMemberRole.Owner,
        });

        application.Status = StoreApplicationStatus.Approved;
        application.ReviewedAt = DateTimeOffset.UtcNow;
        application.ReviewedBy = adminId;

        auditLog.Add(
            who: adminId,
            action: "store.application.approve",
            target: "StoreApplication",
            targetId: application.Id,
            tenant: store.Id);

        AddEmailOutbox(
            to: application.Email,
            templateKey: "email.store_application_approved",
            eventType: "email.store_application_approved",
            @params: new Dictionary<string, string>
            {
                ["store_name"] = application.StoreName,
                ["store_slug"] = application.StoreSlug,
            });

        await db.SaveChangesAsync(ct);

        return ToDto(application);
    }

    /// <inheritdoc/>
    public async Task<StoreApplicationDto> RejectAsync(Guid id, RejectStoreApplicationRequest request, CancellationToken ct)
    {
        var adminId = currentUser.UserId ?? throw new UnauthorizedException();

        var application = await db.StoreApplications.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("找不到開店申請。");

        if (application.Status != StoreApplicationStatus.Pending)
            throw new ValidationException("僅待審核的申請可駁回。");

        application.Status = StoreApplicationStatus.Rejected;
        application.ReviewedAt = DateTimeOffset.UtcNow;
        application.ReviewedBy = adminId;
        application.ReviewComment = request.ReviewComment;

        auditLog.Add(
            who: adminId,
            action: "store.application.reject",
            target: "StoreApplication",
            targetId: application.Id,
            tenant: null);

        AddEmailOutbox(
            to: application.Email,
            templateKey: "email.store_application_rejected",
            eventType: "email.store_application_rejected",
            @params: new Dictionary<string, string>
            {
                ["store_name"] = application.StoreName,
                ["store_slug"] = application.StoreSlug,
                ["review_comment"] = request.ReviewComment,
            });

        await db.SaveChangesAsync(ct);

        return ToDto(application);
    }

    private void AddEmailOutbox(string to, string templateKey, string eventType, Dictionary<string, string> @params)
    {
        var outbox = new OutboxMessage { EventType = eventType };
        outbox.Payload = JsonSerializer.Serialize(new EmailRequestedEvent(
            OutboxMessageId: outbox.Id,
            To:              to,
            TemplateKey:     templateKey,
            Params:          @params,
            Locale:          "zh-TW"));

        db.OutboxMessages.Add(outbox);
    }

    private static StoreApplicationDto ToDto(StoreApplication a) => new()
    {
        Id = a.Id,
        UserId = a.UserId,
        Email = a.Email,
        StoreName = a.StoreName,
        StoreSlug = a.StoreSlug,
        Status = a.Status,
        CreatedAt = a.CreatedAt,
        ReviewedAt = a.ReviewedAt,
        ReviewedBy = a.ReviewedBy,
        ReviewComment = a.ReviewComment,
    };
}
