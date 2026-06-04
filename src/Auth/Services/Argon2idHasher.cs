using System.Security.Cryptography;
using Konscious.Security.Cryptography;

namespace Auth.Services;

/// <summary>
/// 以 Argon2id 實作的密碼雜湊服務。
/// 參數依 OWASP 建議：memory ≥ 19 MB、iterations ≥ 2、parallelism = 1。
/// 雜湊格式：{salt_hex}:{hash_hex}
/// </summary>
public sealed class Argon2idHasher : IPasswordHasher
{
    private const int SaltSize       = 16;
    private const int HashSize       = 32;
    private const int MemorySize     = 19456; // KB (19 MB)
    private const int Iterations     = 2;
    private const int DegreeOfParallelism = 1;

    /// <inheritdoc/>
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);
        return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
    }

    /// <inheritdoc/>
    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(':', 2);
        if (parts.Length != 2) return false;

        try
        {
            var salt = Convert.FromHexString(parts[0]);
            var expected = Convert.FromHexString(parts[1]);
            var actual   = ComputeHash(password, salt);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt                  = salt,
            DegreeOfParallelism   = DegreeOfParallelism,
            MemorySize            = MemorySize,
            Iterations            = Iterations,
        };
        return argon2.GetBytes(HashSize);
    }
}
