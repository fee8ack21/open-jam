using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Services.NotificationRequests;

namespace NotificationService.Controllers;

/// <summary>通知任務 API：商店公告建立（可預定發送時間）、列表、取消。僅商店 Owner 可操作。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/notification-requests")]
public class NotificationRequestsController(INotificationRequestService requestService) : ControllerBase
{
    /// <summary>建立商店公告通知任務；ScheduledAt 為 null 表示立即發送。</summary>
    /// <param name="request">商店、公告內容與預定發送時間。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost]
    [ProducesResponseType<NotificationRequestDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<NotificationRequestDto>> Create(
        CreateNotificationRequestRequest request, CancellationToken ct)
    {
        var created = await requestService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(List), new { storeId = created.StoreId, version = "1.0" }, created);
    }

    /// <summary>查詢商店的通知任務列表（分頁，可依狀態過濾）。</summary>
    /// <param name="request">商店、狀態與分頁條件。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet]
    [ProducesResponseType<ListNotificationRequestsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListNotificationRequestsResponse>> List(
        [FromQuery] ListNotificationRequestsRequest request, CancellationToken ct) =>
        Ok(await requestService.ListAsync(request, ct));

    /// <summary>取消尚未發送的通知任務（僅 Pending 可取消）。</summary>
    /// <param name="id">通知任務 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await requestService.CancelAsync(id, ct);
        return NoContent();
    }
}
