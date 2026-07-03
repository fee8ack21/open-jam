using NotificationService.Models;

namespace NotificationService.Services.NotificationRequests;

/// <summary>通知任務（商店公告 / 預定通知）管理相關業務邏輯。</summary>
public interface INotificationRequestService
{
    /// <summary>建立商店公告通知任務（可預定發送時間）。僅商店 Owner 可操作。</summary>
    Task<NotificationRequestDto> CreateAsync(CreateNotificationRequestRequest request, CancellationToken ct);

    /// <summary>查詢商店的通知任務列表（分頁）。僅商店 Owner 可操作。</summary>
    Task<ListNotificationRequestsResponse> ListAsync(ListNotificationRequestsRequest request, CancellationToken ct);

    /// <summary>取消尚未發送的通知任務。僅商店 Owner 可操作，僅 Pending 可取消。</summary>
    Task CancelAsync(Guid id, CancellationToken ct);
}
