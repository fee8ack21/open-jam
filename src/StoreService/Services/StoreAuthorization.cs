using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using StoreService.Data;
using StoreService.Data.Entities;

namespace StoreService.Services;

/// <summary>商店成員權限檢查共用邏輯。</summary>
public static class StoreAuthorization
{
    /// <summary>確認使用者為該商店的 Owner，否則拋出 <see cref="ForbiddenException"/>。</summary>
    public static async Task EnsureOwnerAsync(StoreDbContext db, Guid storeId, Guid userId, CancellationToken ct)
    {
        var isOwner = await db.StoreMembers.AsNoTracking()
            .AnyAsync(m => m.StoreId == storeId && m.UserId == userId && m.Role == StoreMemberRole.Owner, ct);

        if (!isOwner)
            throw new ForbiddenException();
    }
}
