namespace Auth.Services;

public interface IUserService
{
    Task<(bool Success, string? Error)> RegisterAsync(string email, string password);
    Task<(bool Success, string? Error)> SendPasswordResetAsync(string email);
}
