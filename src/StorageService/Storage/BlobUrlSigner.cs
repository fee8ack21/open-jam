using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StorageService.Options;

namespace StorageService.Storage;

/// <summary>
/// 地端本地儲存的 blob URL 簽章器：以 HMAC-SHA256 對「方法 + 鍵值 + 到期時間」簽章，
/// 扮演雲端 presigned URL 在地端的對應角色。簽發端（<see cref="LocalStorageProvider"/>）與
/// 驗證端（blob 端點）共用同一密鑰，確保上傳 / 下載 URL 無法被竄改或重放。
/// </summary>
public class BlobUrlSigner(IOptions<StorageOptions> options)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(options.Value.Local.SigningKey);

    /// <summary>對指定請求簽章，回傳 base64url 編碼的簽章字串。</summary>
    /// <param name="method">HTTP 方法（"PUT" / "GET"），大小寫不敏感。</param>
    /// <param name="key">物件鍵值（路徑）。</param>
    /// <param name="expiresUnix">到期時間（Unix 秒）。</param>
    public string Sign(string method, string key, long expiresUnix)
    {
        var payload = $"{method.ToUpperInvariant()}\n{key}\n{expiresUnix}";
        var hash = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    /// <summary>驗證簽章是否有效且未過期（固定時間比較，避免時序攻擊）。</summary>
    public bool Verify(string method, string key, long expiresUnix, string? signature)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresUnix)
            return false;

        var expected = Sign(method, key, expiresUnix);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
