using System.Text.RegularExpressions;

namespace Auth.Common.Validators;

public static class PasswordValidator
{
    public const int MinLength = 8;
    public const int MaxLength = 20;

    private static readonly Regex _specialChar =
        new(@"[!""#$%&'()*+,\-./:;<=>?@\[\\\]^_`{|}~]", RegexOptions.Compiled);
    private static readonly Regex _fullWidth =
        new(@"[^\x00-\x7F]", RegexOptions.Compiled);

    public static bool IsValid(string? password, out string? error)
    {
        error = null;
        if (string.IsNullOrEmpty(password))   { error = "請設定密碼"; return false; }
        if (password.Length < MinLength)       { error = $"密碼至少需要 {MinLength} 個字元"; return false; }
        if (password.Length > MaxLength)       { error = $"密碼最多 {MaxLength} 個字元"; return false; }
        if (!password.Any(char.IsUpper))       { error = "密碼需包含大寫英文字母 (A–Z)"; return false; }
        if (!password.Any(char.IsLower))       { error = "密碼需包含小寫英文字母 (a–z)"; return false; }
        if (!password.Any(char.IsDigit))       { error = "密碼需包含數字 (0–9)"; return false; }
        if (!_specialChar.IsMatch(password))   { error = "密碼需包含特殊符號（例如 !@#$）"; return false; }
        if (password.Any(char.IsWhiteSpace))   { error = "密碼不得包含空白字元"; return false; }
        if (_fullWidth.IsMatch(password))      { error = "密碼只能使用半形字元"; return false; }
        return true;
    }
}
