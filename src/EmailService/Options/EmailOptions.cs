namespace EmailService.Options;

/// <summary>EmailRetryService 行為設定。</summary>
public class EmailOptions
{
    /// <summary>最大重試次數；超過後停止補償重試，紀錄留存 Failed 狀態。</summary>
    /// <example>5</example>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>EmailRetryService 掃描間隔（分鐘）。</summary>
    /// <example>2</example>
    public int RetryIntervalMinutes { get; set; } = 2;
}
