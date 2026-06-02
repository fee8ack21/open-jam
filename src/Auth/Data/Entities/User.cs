namespace Auth.Data.Entities;

public class User
{
    public Guid     Id           { get; set; }
    public string   Email        { get; set; } = "";
    public string   PasswordHash { get; set; } = "";
    public bool     IsVerified   { get; set; }
    public DateTime CreatedAt    { get; set; }

    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = [];
    public ICollection<PasswordResetToken>     PasswordResetTokens     { get; set; } = [];
}
