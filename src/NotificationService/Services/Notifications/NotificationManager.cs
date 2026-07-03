using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;
using Shared.Auth;
using Shared.Exceptions;

namespace NotificationService.Services.Notifications;

/// <summary>In-app 通知業務邏輯實作。</summary>
public class NotificationManager(
    NotificationDbContext db,
    ICurrentUserAccessor currentUser,
    IMapper mapper) : INotificationManager
{
    /// <inheritdoc/>
    public async Task<ListNotificationsResponse> ListMineAsync(ListNotificationsRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var query = db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == userId);

        if (request.UnreadOnly)
            query = query.Where(n => n.ReadAt == null);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ProjectTo<NotificationDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new ListNotificationsResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task<UnreadCountResponse> GetUnreadCountAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var count = await db.Notifications
            .CountAsync(n => n.RecipientUserId == userId && n.ReadAt == null, ct);

        return new UnreadCountResponse { Count = count };
    }

    /// <inheritdoc/>
    public async Task MarkReadAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId, ct)
            ?? throw new NotFoundException("找不到通知。");

        if (notification.ReadAt is null)
        {
            notification.ReadAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task MarkAllReadAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        await db.Notifications
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow), ct);
    }
}
