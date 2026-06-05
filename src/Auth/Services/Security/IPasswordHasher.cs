namespace Auth.Services.Security;

/// <summary>密碼雜湊服務介面，封裝雜湊演算法，使業務層與具體實作解耦。</summary>
public interface IPasswordHasher
{
    /// <summary>將明文密碼雜湊成儲存用字串（含 salt）。</summary>
    /// <param name="password">明文密碼。</param>
    /// <returns>雜湊結果字串。</returns>
    string Hash(string password);

    /// <summary>驗證明文密碼是否與雜湊結果相符。</summary>
    /// <param name="password">明文密碼。</param>
    /// <param name="hash">儲存的雜湊字串。</param>
    /// <returns>相符回傳 true，否則 false。</returns>
    bool Verify(string password, string hash);
}
