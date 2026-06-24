namespace OrderService.Services;

/// <summary>產生對外顯示的人類可讀訂單編號，格式 OJ-yyyyMMdd-XXXXXXXX（8 碼大寫 16 進位）。</summary>
public static class OrderNumberGenerator
{
    public static string Next()
    {
        var date    = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var suffix  = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"OJ-{date}-{suffix}";
    }
}
