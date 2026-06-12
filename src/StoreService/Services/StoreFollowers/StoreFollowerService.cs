using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Exceptions;
using StoreService.Data;
using StoreService.Data.Entities;
using StoreService.Models;

namespace StoreService.Services.StoreFollowers;

/// <summary>商店追蹤者業務邏輯實作。</summary>
public class StoreFollowerService(StoreDbContext db, ICurrentUserAccessor currentUser) : IStoreFollowerService
{
    /// <inheritdoc/>
    public async Task FollowAsync(Guid storeId, FollowStoreRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        if (!IsValidEmail(email))
            throw new ValidationException("信箱格式錯誤。");

        var storeExists = await db.Stores.AnyAsync(s => s.Id == storeId, ct);
        if (!storeExists)
            throw new NotFoundException("找不到商店。");

        var alreadyFollowing = await db.StoreFollowers
            .AnyAsync(f => f.StoreId == storeId && f.Email == email, ct);

        if (!alreadyFollowing)
        {
            db.StoreFollowers.Add(new StoreFollower
            {
                StoreId = storeId,
                UserId = currentUser.UserId,
                Email = email,
            });

            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task UnfollowAsync(Guid storeId, FollowStoreRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();

        var follower = await db.StoreFollowers
            .FirstOrDefaultAsync(f => f.StoreId == storeId && f.Email == email, ct);

        if (follower is not null)
        {
            db.StoreFollowers.Remove(follower);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task<GetStoreFollowersResponse> GetFollowersAsync(
        Guid storeId, GetStoreFollowersRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var storeExists = await db.Stores.AnyAsync(s => s.Id == storeId, ct);
        if (!storeExists)
            throw new NotFoundException("找不到商店。");

        await StoreAuthorization.EnsureOwnerAsync(db, storeId, userId, ct);

        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = db.StoreFollowers.AsNoTracking().Where(f => f.StoreId == storeId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(request.Offset)
            .Take(limit)
            .Select(f => new StoreFollowerDto
            {
                Email = f.Email,
                UserId = f.UserId,
                CreatedAt = f.CreatedAt,
            })
            .ToListAsync(ct);

        return new GetStoreFollowersResponse { TotalCount = total, Items = items };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new System.Net.Mail.MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
