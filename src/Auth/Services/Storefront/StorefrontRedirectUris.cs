using System.Text.Json.Nodes;

namespace Auth.Services.Storefront;

/// <summary>
/// 店面子網域 OIDC redirect URI 的組裝與合併邏輯。
/// Hydra 不支援萬用字元 redirect URI，每個店面 origin 的 callback / silent-renew /
/// post-logout URI 都須逐一列入 web client 白名單；此處集中「URL 樣式 → URI 清單」
/// 與「合併進 client JSON」的規則，供 Auth consumer 與 Bootstrap 回填 seeder 共用。
/// </summary>
public static class StorefrontRedirectUris
{
    /// <summary>店面根 URL 樣式的預設值；{storeSlug} 為子網域佔位符。</summary>
    public const string DefaultUrlPattern = "https://{storeSlug}.openjam.co/";

    /// <summary>
    /// 將指定店面 slug 的 redirect / post-logout URI 合併進 Hydra client JSON
    /// （缺漏才追加，重複呼叫冪等）。回傳是否有任何變更（呼叫端據此決定是否 PUT 回 Hydra）。
    /// </summary>
    /// <param name="client">GetClientAsync 取回的 client 完整 JSON。</param>
    /// <param name="urlPattern">店面根 URL 樣式，含 {storeSlug} 佔位符。</param>
    /// <param name="storeSlug">商店子網域 slug。</param>
    public static bool MergeInto(JsonNode client, string urlPattern, string storeSlug)
    {
        var baseUrl = urlPattern.Replace("{storeSlug}", storeSlug).TrimEnd('/') + "/";

        var changed = AppendMissing(client, "redirect_uris", [$"{baseUrl}callback.html", $"{baseUrl}silent-renew.html"]);
        changed |= AppendMissing(client, "post_logout_redirect_uris", [baseUrl]);
        return changed;
    }

    private static bool AppendMissing(JsonNode client, string property, string[] uris)
    {
        if (client[property] is not JsonArray array)
        {
            array = [];
            client[property] = array;
        }

        var existing = array.Select(n => n?.GetValue<string>()).ToHashSet();
        var changed = false;
        foreach (var uri in uris.Where(u => !existing.Contains(u)))
        {
            array.Add(uri);
            changed = true;
        }
        return changed;
    }
}
