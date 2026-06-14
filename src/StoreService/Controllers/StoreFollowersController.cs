using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreService.Models;
using StoreService.Services.StoreFollowers;

namespace StoreService.Controllers;

/// <summary>商店追蹤者 API：追蹤／取消追蹤（公開）、追蹤者列表（Owner）。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/stores/{id:guid}")]
public class StoreFollowersController(IStoreFollowerService followerService) : ControllerBase
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
        await followerService.FollowAsync(id, request, ct);
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
        await followerService.UnfollowAsync(id, request, ct);
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
        Guid id, [FromQuery] GetStoreFollowersRequest request, CancellationToken ct) =>
        Ok(await followerService.GetFollowersAsync(id, request, ct));
}
