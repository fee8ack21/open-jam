using Shared.Audit;

namespace NotificationService.Data.Entities;

/// <summary>
/// In-app 通知（fan-out 產物），僅對已關聯帳號的追蹤者建立；訪客追蹤者只收 Email。
/// </summary>
public class Notification : ICreatedAt
{
    /// <summary>通知唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>來源通知任務 ID。</summary>
    public Guid RequestId { get; set; }

    /// <summary>收件者使用者 ID。</summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>收件者電子信箱（fan-out 當下的追蹤信箱，與 RequestId 組成去重鍵）。</summary>
    public string RecipientEmail { get; set; } = "";

    /// <summary>通知類型（同來源任務的 Type），前端據此搭配 Payload 渲染。</summary>
    public string Type { get; set; } = "";

    /// <summary>通知內容參數（JSON，含商店與商品資訊）。</summary>
    public string Payload { get; set; } = "";

    /// <summary>已讀時間；null 表示未讀。</summary>
    public DateTimeOffset? ReadAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
