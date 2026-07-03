using NotificationService.Data.Entities;

namespace NotificationService.Models;

/// <summary>通知任務單筆資料。</summary>
public class NotificationRequestDto
{
    /// <summary>通知任務 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>通知類型。</summary>
    /// <example>store.announcement</example>
    public string Type { get; set; } = "";

    /// <summary>通知對象商店 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid StoreId { get; set; }

    /// <summary>通知內容參數（JSON 字串，camelCase，依 Type 決定結構）。</summary>
    /// <example>{"title":"夏季特賣開跑","message":"全店素材 8 折，只到月底！"}</example>
    public string Payload { get; set; } = "";

    /// <summary>預定發送時間。</summary>
    public DateTimeOffset ScheduledAt { get; set; }

    /// <summary>任務狀態。</summary>
    /// <example>Pending</example>
    public NotificationRequestStatus Status { get; set; }

    /// <summary>完成 fan-out 的時間；null 表示尚未發送。</summary>
    public DateTimeOffset? DispatchedAt { get; set; }

    /// <summary>建立時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>建立商店公告通知任務（可預定發送時間）。</summary>
public class CreateNotificationRequestRequest
{
    /// <summary>通知對象商店 ID（fan-out 至該商店的追蹤者）。僅商店 Owner 可建立。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid StoreId { get; set; }

    /// <summary>公告標題。</summary>
    /// <example>夏季特賣開跑</example>
    public string Title { get; set; } = "";

    /// <summary>公告內文。</summary>
    /// <example>全店素材 8 折，只到月底！</example>
    public string Message { get; set; } = "";

    /// <summary>預定發送時間；null 表示立即發送。</summary>
    public DateTimeOffset? ScheduledAt { get; set; }
}

/// <summary>查詢商店通知任務列表的條件。</summary>
public class ListNotificationRequestsRequest
{
    /// <summary>商店 ID（必填，僅該商店 Owner 可查詢）。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid StoreId { get; set; }

    /// <summary>以任務狀態過濾；null 表示不過濾。</summary>
    /// <example>Pending</example>
    public NotificationRequestStatus? Status { get; set; }

    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; }

    /// <summary>取回筆數上限。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;
}

/// <summary>商店通知任務列表回應。</summary>
public class ListNotificationRequestsResponse
{
    /// <summary>符合條件的總筆數。</summary>
    /// <example>7</example>
    public int TotalCount { get; set; }

    /// <summary>本頁任務。</summary>
    public List<NotificationRequestDto> Items { get; set; } = [];
}
