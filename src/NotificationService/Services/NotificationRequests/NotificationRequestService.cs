using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Data.Entities;
using NotificationService.Models;
using Shared.Auth;
using Shared.Exceptions;

namespace NotificationService.Services.NotificationRequests;

/// <summary>通知任務管理業務邏輯實作。</summary>
public class NotificationRequestService(
    NotificationDbContext db,
    ICurrentUserAccessor currentUser,
    StoreServiceClient storeClient,
    IMapper mapper) : INotificationRequestService
{
    /// <inheritdoc/>
    public async Task<NotificationRequestDto> CreateAsync(CreateNotificationRequestRequest request, CancellationToken ct)
    {
        _ = currentUser.UserId ?? throw new UnauthorizedException();
        await storeClient.EnsureStoreOwnerAsync(request.StoreId, ct);

        var entity = new NotificationRequest
        {
            Type        = NotificationTypes.StoreAnnouncement,
            StoreId     = request.StoreId,
            ScheduledAt = request.ScheduledAt ?? DateTimeOffset.UtcNow,
            Payload = JsonSerializer.Serialize(new StoreAnnouncementPayload
            {
                Title   = request.Title.Trim(),
                Message = request.Message.Trim(),
            }, PayloadJson.Options),
        };
        db.NotificationRequests.Add(entity);

        await db.SaveChangesAsync(ct);

        return mapper.Map<NotificationRequestDto>(entity);
    }

    /// <inheritdoc/>
    public async Task<ListNotificationRequestsResponse> ListAsync(ListNotificationRequestsRequest request, CancellationToken ct)
    {
        _ = currentUser.UserId ?? throw new UnauthorizedException();
        await storeClient.EnsureStoreOwnerAsync(request.StoreId, ct);

        var query = db.NotificationRequests.AsNoTracking()
            .Where(r => r.StoreId == request.StoreId);

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.ScheduledAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ProjectTo<NotificationRequestDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new ListNotificationRequestsResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task CancelAsync(Guid id, CancellationToken ct)
    {
        _ = currentUser.UserId ?? throw new UnauthorizedException();

        var request = await db.NotificationRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("找不到通知任務。");

        await storeClient.EnsureStoreOwnerAsync(request.StoreId, ct);

        // 條件式更新避免與 dispatcher 併發競態：發送中（持鎖）會阻塞至完成，屆時狀態已非 Pending 而更新 0 筆
        var cancelled = await db.NotificationRequests
            .Where(r => r.Id == id && r.Status == NotificationRequestStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, NotificationRequestStatus.Cancelled), ct);

        if (cancelled == 0)
            throw new ConflictException("僅待發送的通知任務可取消。");
    }
}
