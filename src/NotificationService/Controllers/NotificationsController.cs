using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Services.Notifications;

namespace NotificationService.Controllers;

/// <summary>In-app 通知 API：本人通知列表、未讀數、標記已讀。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/notifications")]
public class NotificationsController(INotificationManager notificationManager) : ControllerBase
{
    /// <summary>查詢登入使用者本人的通知列表（分頁，可僅未讀）。</summary>
    /// <param name="request">分頁與未讀過濾條件。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("mine")]
    [ProducesResponseType<ListNotificationsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListNotificationsResponse>> ListMine(
        [FromQuery] ListNotificationsRequest request, CancellationToken ct) =>
        Ok(await notificationManager.ListMineAsync(request, ct));

    /// <summary>查詢登入使用者本人的未讀通知數（前端鈴鐺輪詢用）。</summary>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("mine/unread-count")]
    [ProducesResponseType<UnreadCountResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadCountResponse>> GetUnreadCount(CancellationToken ct) =>
        Ok(await notificationManager.GetUnreadCountAsync(ct));

    /// <summary>將本人的單筆通知標為已讀。已讀則 no-op。</summary>
    /// <param name="id">通知 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await notificationManager.MarkReadAsync(id, ct);
        return NoContent();
    }

    /// <summary>將本人的全部未讀通知標為已讀。</summary>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await notificationManager.MarkAllReadAsync(ct);
        return NoContent();
    }
}
