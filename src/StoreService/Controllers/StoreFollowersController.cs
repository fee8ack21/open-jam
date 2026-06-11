using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Exceptions;
using StoreService.Data;
using StoreService.Data.Entities;
using StoreService.Models;
using StoreService.Services;

namespace StoreService.Controllers;

/// <summary>商店追蹤者 API：追蹤／取消追蹤（公開）、追蹤者列表（Owner）。</summary>
[ApiController]
[Route("stores/{id:guid}")]
public class StoreFollowersController(StoreDbContext db, ICurrentUserAccessor currentUser) : ControllerBase
{
    /// <summary>追蹤商店。已追蹤則 no-op。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="request">追蹤者信箱。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("follow")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FollowAsync(Guid id, [FromBody] FollowStoreRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        if (!IsValidEmail(email))
            throw new ValidationException("信箱格式錯誤。");

        var storeExists = await db.Stores.AnyAsync(s => s.Id == id, ct);
        if (!storeExists)
            throw new NotFoundException("找不到商店。");

        var alreadyFollowing = await db.StoreFollowers
            .AnyAsync(f => f.StoreId == id && f.Email == email, ct);

        if (!alreadyFollowing)
        {
            db.StoreFollowers.Add(new StoreFollower
            {
                StoreId = id,
                UserId = currentUser.UserId,
                Email = email,
            });

            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    /// <summary>取消追蹤商店。依 (StoreId, Email) 移除，未追蹤則 no-op。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="request">追蹤者信箱。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("follow")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnfollowAsync(Guid id, [FromBody] FollowStoreRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();

        var follower = await db.StoreFollowers
            .FirstOrDefaultAsync(f => f.StoreId == id && f.Email == email, ct);

        if (follower is not null)
        {
            db.StoreFollowers.Remove(follower);
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    /// <summary>查詢商店追蹤者列表（分頁）。僅 Owner 可操作。</summary>
    /// <param name="id">商店 ID。</param>
    /// <param name="request">分頁參數。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("followers")]
    [Authorize]
    [ProducesResponseType<GetStoreFollowersResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GetStoreFollowersResponse>> GetFollowersAsync(
        Guid id, [FromQuery] GetStoreFollowersRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var storeExists = await db.Stores.AnyAsync(s => s.Id == id, ct);
        if (!storeExists)
            throw new NotFoundException("找不到商店。");

        await StoreAuthorization.EnsureOwnerAsync(db, id, userId, ct);

        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = db.StoreFollowers.AsNoTracking().Where(f => f.StoreId == id);

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

        return Ok(new GetStoreFollowersResponse { TotalCount = total, Items = items });
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
