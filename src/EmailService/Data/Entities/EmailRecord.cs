namespace EmailService.Data.Entities;

public enum EmailStatus { Pending, Sent, Failed }

public class EmailRecord
{
    public Guid        Id              { get; set; }
    public Guid        OutboxMessageId { get; set; }
    public string      To              { get; set; } = "";
    public string      Subject         { get; set; } = "";
    public string      BodyHtml        { get; set; } = "";
    public EmailStatus Status          { get; set; }
    public int         AttemptCount    { get; set; }
    public DateTime?   LastAttemptAt   { get; set; }
    public DateTime?   SentAt          { get; set; }
    public string?     ErrorMessage    { get; set; }
    public DateTime    CreatedAt       { get; set; }
}
