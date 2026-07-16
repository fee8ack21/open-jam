using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace CatalogService.Services;

/// <summary>YouTube 影片 URL 解析與正規化（外部影片嵌入資產用）。</summary>
public static partial class YouTubeUrl
{
    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex VideoIdPattern();

    private static readonly HashSet<string> Hosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com",
        "youtube-nocookie.com", "www.youtube-nocookie.com",
    };

    /// <summary>
    /// 嘗試由 YouTube 網址解析出影片 ID。支援 watch?v= / youtu.be / shorts / embed / live 形式。
    /// </summary>
    public static bool TryParseVideoId(string? url, out string videoId)
    {
        videoId = "";
        if (string.IsNullOrWhiteSpace(url))
            return false;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        string? candidate = null;
        var segments = uri.AbsolutePath.Trim('/').Split('/');

        if (string.Equals(uri.Host, "youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            candidate = segments.FirstOrDefault();
        }
        else if (Hosts.Contains(uri.Host))
        {
            if (segments.Length >= 1 && segments[0] == "watch")
                candidate = QueryHelpers.ParseQuery(uri.Query).TryGetValue("v", out var v) ? v.ToString() : null;
            else if (segments.Length >= 2 && segments[0] is "shorts" or "embed" or "live")
                candidate = segments[1];
        }

        if (candidate is null || !VideoIdPattern().IsMatch(candidate))
            return false;

        videoId = candidate;
        return true;
    }

    /// <summary>由影片 ID 組出正規化的觀看網址（統一儲存格式）。</summary>
    public static string CanonicalUrl(string videoId) => $"https://www.youtube.com/watch?v={videoId}";
}
