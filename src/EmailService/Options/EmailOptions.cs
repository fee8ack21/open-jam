namespace EmailService.Options;

public class EmailOptions
{
    public int MaxRetryAttempts     { get; set; } = 5;
    public int RetryIntervalMinutes { get; set; } = 2;
}
