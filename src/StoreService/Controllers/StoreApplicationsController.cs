using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Events;
using Shared.Exceptions;
using StoreService.Data;
using StoreService.Data.Entities;
using StoreService.Models;
using StoreService.Services;

namespace StoreService.Controllers;

/// <summary>開店申請 API：提交、查詢、撤回，以及 Admin 審核（核准／駁回）。</summary>
[ApiController]
[Route("store-applications")]
[Authorize]
public class StoreApplicationsController(StoreDbContext db, ICurrentUserAccessor currentUser) : ControllerBase
{
    /// <summary>提交開店申請。同一使用者僅能有一筆 Pending 申請。</summary>
    /// <param name="request">商店顯示名稱與子網域代稱。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>建立後的申請紀錄。</returns>
    [HttpPost]
    [ProducesResponseType<StoreApplicationDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<StoreApplicationDto>> SubmitAsync(
        [FromBody] SubmitStoreApplicationRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? throw new UnauthorizedException();

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

        AddAuditLogOutbox(
            who: userId,
            action: "store.application.submit",
            targetId: application.Id,
            tenant: null);

        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, ToDto(application));
    }

    /// <summary>查詢自己的開店申請紀錄（分頁）。</summary>
    /// <param name="request">分頁參數。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("me")]
    [ProducesResponseType<GetStoreApplicationsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GetStoreApplicationsResponse>> GetMineAsync(
        [FromQuery] GetStoreApplicationsRequest request, CancellationToken ct)
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

        return Ok(new GetStoreApplicationsResponse { TotalCount = total, Items = items });
    }

    /// <summary>撤回自己的待審核申請（Pending → Withdrawn），可重新提交。</summary>
    /// <param name="id">申請 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> WithdrawAsync(Guid id, CancellationToken ct)
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

        return NoContent();
    }

    /// <summary>查詢全平台開店申請列表，可依審核狀態篩選（分頁）。</summary>
    /// <param name="request">篩選與分頁參數。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<GetStoreApplicationsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GetStoreApplicationsResponse>> GetAllAsync(
        [FromQuery] GetStoreApplicationsRequest request, CancellationToken ct)
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

        return Ok(new GetStoreApplicationsResponse { TotalCount = total, Items = items });
    }

    /// <summary>核准開店申請（Pending → Approved），建立 Store 與 StoreMember(Owner)。</summary>
    /// <param name="id">申請 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<StoreApplicationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreApplicationDto>> ApproveAsync(Guid id, CancellationToken ct)
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

        AddAuditLogOutbox(
            who: adminId,
            action: "store.application.approve",
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

        return Ok(ToDto(application));
    }

    /// <summary>駁回開店申請（Pending → Rejected），可重新提交。</summary>
    /// <param name="id">申請 ID。</param>
    /// <param name="request">駁回原因。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<StoreApplicationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreApplicationDto>> RejectAsync(
        Guid id, [FromBody] RejectStoreApplicationRequest request, CancellationToken ct)
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

        AddAuditLogOutbox(
            who: adminId,
            action: "store.application.reject",
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

        return Ok(ToDto(application));
    }

    private void AddAuditLogOutbox(Guid? who, string action, Guid targetId, Guid? tenant)
    {
        var outbox = new OutboxMessage { EventType = "audit." + action };
        outbox.Payload = JsonSerializer.Serialize(new AuditLogRequestedEvent(
            OutboxMessageId: outbox.Id,
            Who:             who,
            Action:          action,
            Target:          "StoreApplication",
            TargetId:        targetId,
            Result:          "success",
            Before:          null,
            After:           null,
            Ip:              HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent:       HttpContext.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null,
            Tenant:          tenant,
            OccurredAt:      DateTimeOffset.UtcNow,
            CorrelationId:   HttpContext.TraceIdentifier));

        db.OutboxMessages.Add(outbox);
    }

    private void AddEmailOutbox(string to, string templateKey, string eventType, Dictionary<string, string> @params)
    {
        var outbox = new OutboxMessage { EventType = eventType };
        outbox.Payload = JsonSerializer.Serialize(new EmailRequestedEvent(
            OutboxMessageId: outbox.Id,
            To:              to,
            TemplateKey:     templateKey,
            Params:          @params,
            Locale:          "zh-TW",
            EventType:       eventType));

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
