namespace NotificationService.Models;

/// <summary>In-app 通知單筆資料。</summary>
public class NotificationDto
{
    /// <summary>通知 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>通知類型，前端據此搭配 Payload 渲染。</summary>
    /// <example>catalog.published</example>
    public string Type { get; set; } = "";

    /// <summary>通知內容參數（JSON 字串，camelCase，依 Type 決定結構）。</summary>
    /// <example>{"catalogId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","catalogName":"插畫素材包","storeName":"小明的數位商店"}</example>
    public string Payload { get; set; } = "";

    /// <summary>已讀時間；null 表示未讀。</summary>
    public DateTimeOffset? ReadAt { get; set; }

    /// <summary>通知建立時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>查詢本人通知列表的條件。</summary>
public class ListNotificationsRequest
{
    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; }

    /// <summary>取回筆數上限。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;

    /// <summary>是否僅列出未讀通知。</summary>
    /// <example>false</example>
    public bool UnreadOnly { get; set; }
}

/// <summary>本人通知列表回應。</summary>
public class ListNotificationsResponse
{
    /// <summary>符合條件的總筆數。</summary>
    /// <example>42</example>
    public int TotalCount { get; set; }

    /// <summary>本頁通知。</summary>
    public List<NotificationDto> Items { get; set; } = [];
}

/// <summary>未讀通知數回應。</summary>
public class UnreadCountResponse
{
    /// <summary>未讀通知數。</summary>
    /// <example>3</example>
    public int Count { get; set; }
}
