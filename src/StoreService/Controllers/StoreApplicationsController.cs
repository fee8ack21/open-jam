using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreService.Models;
using StoreService.Services.StoreApplications;

namespace StoreService.Controllers;

/// <summary>開店申請 API：提交、查詢、撤回，以及 Admin 審核（核准／駁回）。</summary>
[ApiController]
[Route("store-applications")]
[Authorize]
public class StoreApplicationsController(IStoreApplicationService applicationService) : ControllerBase
{
    /// <summary>提交開店申請。同一使用者僅能有一筆 Pending 申請。</summary>
    /// <param name="request">商店顯示名稱與子網域代稱。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>建立後的申請紀錄。</returns>
    [HttpPost]
    [ProducesResponseType<StoreApplicationDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<StoreApplicationDto>> SubmitAsync(
        [FromBody] SubmitStoreApplicationRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await applicationService.SubmitAsync(request, ct));

    /// <summary>查詢自己的開店申請紀錄（分頁）。</summary>
    /// <param name="request">分頁參數。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("me")]
    [ProducesResponseType<GetStoreApplicationsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GetStoreApplicationsResponse>> GetMineAsync(
        [FromQuery] GetStoreApplicationsRequest request, CancellationToken ct) =>
        Ok(await applicationService.GetMineAsync(request, ct));

    /// <summary>撤回自己的待審核申請（Pending → Withdrawn），可重新提交。</summary>
    /// <param name="id">申請 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> WithdrawAsync(Guid id, CancellationToken ct)
    {
        await applicationService.WithdrawAsync(id, ct);
        return NoContent();
    }

    /// <summary>查詢全平台開店申請列表，可依審核狀態篩選（分頁）。</summary>
    /// <param name="request">篩選與分頁參數。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<GetStoreApplicationsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GetStoreApplicationsResponse>> GetAllAsync(
        [FromQuery] GetStoreApplicationsRequest request, CancellationToken ct) =>
        Ok(await applicationService.GetAllAsync(request, ct));

    /// <summary>核准開店申請（Pending → Approved），建立 Store 與 StoreMember(Owner)。</summary>
    /// <param name="id">申請 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<StoreApplicationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreApplicationDto>> ApproveAsync(Guid id, CancellationToken ct) =>
        Ok(await applicationService.ApproveAsync(id, ct));

    /// <summary>駁回開店申請（Pending → Rejected），可重新提交。</summary>
    /// <param name="id">申請 ID。</param>
    /// <param name="request">駁回原因。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<StoreApplicationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreApplicationDto>> RejectAsync(
        Guid id, [FromBody] RejectStoreApplicationRequest request, CancellationToken ct) =>
        Ok(await applicationService.RejectAsync(id, request, ct));
}
