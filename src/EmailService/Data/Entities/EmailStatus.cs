namespace EmailService.Data.Entities;

/// <summary>信件寄送狀態。</summary>
public enum EmailStatus
{
    /// <summary>已排入待發送。</summary>
    Pending,

    /// <summary>已成功寄出。</summary>
    Sent,

    /// <summary>發送失敗，等待重試或達上限。</summary>
    Failed,
}
