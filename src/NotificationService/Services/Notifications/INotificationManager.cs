using NotificationService.Models;

namespace NotificationService.Services.Notifications;

/// <summary>In-app 通知查詢與已讀相關業務邏輯。</summary>
public interface INotificationManager
{
    /// <summary>查詢登入使用者本人的通知列表（分頁，可僅未讀）。</summary>
    Task<ListNotificationsResponse> ListMineAsync(ListNotificationsRequest request, CancellationToken ct);

    /// <summary>查詢登入使用者本人的未讀通知數。</summary>
    Task<UnreadCountResponse> GetUnreadCountAsync(CancellationToken ct);

    /// <summary>將本人的單筆通知標為已讀。已讀則 no-op。</summary>
    Task MarkReadAsync(Guid id, CancellationToken ct);

    /// <summary>將本人的全部未讀通知標為已讀。</summary>
    Task MarkAllReadAsync(CancellationToken ct);
}
