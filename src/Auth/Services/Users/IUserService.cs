using Auth.Data.Entities;

namespace Auth.Services.Users;

/// <summary>帳號相關業務邏輯服務介面。</summary>
public interface IUserService
{
    /// <summary>
    /// 註冊新帳號。若同一信箱已有 Pending 帳號（squatting）則覆蓋並重發驗證信；
    /// 若已是 Active 等已驗證狀態則回傳衝突錯誤。
    /// </summary>
    /// <param name="email">電子信箱。</param>
    /// <param name="password">明文密碼。</param>
    /// <returns>(Success, Error)；Success = false 時 Error 包含錯誤訊息。</returns>
    Task<(bool Success, string? Error)> RegisterAsync(string email, string password);

    /// <summary>
    /// 驗證信箱 token，成功後將帳號狀態設為 Active。
    /// Token 過期或已使用皆回傳失敗。
    /// </summary>
    /// <param name="token">驗證連結中的 token 字串。</param>
    /// <returns>(Success, Error)。</returns>
    Task<(bool Success, string? Error)> VerifyEmailAsync(string token);

    /// <summary>
    /// 驗證帳號密碼，回傳帳號 ID（作為 Hydra subject）。
    /// 帳號不存在、密碼錯誤或帳號狀態不允許登入皆回傳失敗。
    /// </summary>
    /// <param name="email">電子信箱。</param>
    /// <param name="password">明文密碼。</param>
    /// <returns>(Success, Subject, Error)；Success = true 時 Subject 為使用者 ID 字串。</returns>
    Task<(bool Success, string? Subject, string? Error)> LoginAsync(string email, string password);

    /// <summary>
    /// 送出忘記密碼請求，若信箱存在則建立重置 token 並寫入 Outbox。
    /// 無論信箱是否存在一律成功回傳，防止帳號列舉。
    /// </summary>
    /// <param name="email">電子信箱。</param>
    /// <returns>(Success, Error)。</returns>
    Task<(bool Success, string? Error)> SendPasswordResetAsync(string email);

    /// <summary>
    /// 以重置 token 更新密碼，成功後將 token 標記為已使用。
    /// Token 過期或已使用皆回傳失敗。
    /// </summary>
    /// <param name="token">重置連結中的 token 字串。</param>
    /// <param name="newPassword">新的明文密碼。</param>
    /// <returns>(Success, Error)。</returns>
    Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword);

    /// <summary>
    /// 取得使用者的 role、email，供 Hydra consent 流程注入 access token claim（ext.role / ext.email）。
    /// </summary>
    /// <param name="subject">使用者 ID（Hydra subject）。</param>
    /// <returns>找不到對應使用者時回傳 null。</returns>
    Task<Dictionary<string, object>?> GetAccessTokenClaimsAsync(string subject);
}
