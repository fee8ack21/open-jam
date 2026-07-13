using Microsoft.AspNetCore.Html;
using System.Globalization;

namespace Auth.Common;

/// <summary>
/// v3「果醬罐 Neo-Brutalism」貼紙 icon：每個 icon 為完整 inner SVG markup（筆觸已內建）。
/// 單色 icon 用 currentColor 跟隨文字色；多色貼紙 icon（lock / eye / star / alert / image）固定糖果色。
/// </summary>
public static class IconHelper
{
    private sealed record Icon(string ViewBox, string Body);

    private static readonly Dictionary<string, Icon> Icons = new()
    {
        ["mail"] = new("0 0 24 24",
            """<rect x="3" y="5" width="18" height="14" rx="3" fill="none" stroke="currentColor" stroke-width="2.2"></rect><path d="M4 7 l8 6 8 -6" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"></path>"""),
        ["lock"] = new("0 0 24 24",
            """<rect x="5" y="10.5" width="14" height="10" rx="3" fill="#FFDE00" stroke="currentColor" stroke-width="2.2"></rect><path d="M8 10.5 V8 a4 4 0 0 1 8 0 v2.5" fill="none" stroke="currentColor" stroke-width="2.2"></path><circle cx="12" cy="15.5" r="1.7" fill="currentColor"></circle>"""),
        ["eye"] = new("0 0 24 24",
            """<path d="M2.5 12 c2.5 -4.6 5.7 -6.9 9.5 -6.9 s7 2.3 9.5 6.9 c-2.5 4.6 -5.7 6.9 -9.5 6.9 s-7 -2.3 -9.5 -6.9 z" fill="#FFFFFF" stroke="currentColor" stroke-width="2"></path><circle cx="12" cy="12" r="3" fill="currentColor"></circle>"""),
        ["eyeOff"] = new("0 0 24 24",
            """<path d="M2.5 12 c2.5 -4.6 5.7 -6.9 9.5 -6.9 s7 2.3 9.5 6.9 c-2.5 4.6 -5.7 6.9 -9.5 6.9 s-7 -2.3 -9.5 -6.9 z" fill="#FFFFFF" stroke="currentColor" stroke-width="2"></path><circle cx="12" cy="12" r="3" fill="currentColor"></circle><path d="M4.5 20.5 L19.5 3.5" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"></path>"""),
        ["check"] = new("0 0 24 24",
            """<path d="M4.5 12.5 l5 5 10 -11" fill="none" stroke="currentColor" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round"></path>"""),
        ["x"] = new("0 0 24 24",
            """<path d="M6 6 l12 12 M18 6 L6 18" stroke="currentColor" stroke-width="2.8" stroke-linecap="round"></path>"""),
        ["arrowLeft"] = new("0 0 24 24",
            """<path d="M19.5 12 H6 M11.5 6 l-6 6 6 6" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"></path>"""),
        ["arrowRight"] = new("0 0 24 24",
            """<path d="M4.5 12 h13.5 M12.5 6 l6 6 -6 6" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"></path>"""),
        ["chevronDown"] = new("0 0 24 24",
            """<path d="M5 9 l7 7 7 -7" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"></path>"""),
        ["alert"] = new("0 0 24 24",
            """<path d="M10.1 4.2 L2.6 17.5 a2.2 2.2 0 0 0 1.9 3.3 h15 a2.2 2.2 0 0 0 1.9 -3.3 L13.9 4.2 a2.2 2.2 0 0 0 -3.8 0 z" fill="#FFDE00" stroke="currentColor" stroke-width="2.2" stroke-linejoin="round"></path><path d="M12 9.5 v4.5" stroke="currentColor" stroke-width="2.6" stroke-linecap="round"></path><circle cx="12" cy="17.2" r="1.4" fill="currentColor"></circle>"""),
        ["shield"] = new("0 0 24 24",
            """<path d="M12 3 l8 3 v5 c0 5 -3.4 8.5 -8 10 c-4.6 -1.5 -8 -5 -8 -10 V6 z" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linejoin="round"></path><path d="M8.5 11.8 l2.5 2.5 4.5 -5" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"></path>"""),
        ["sparkle"] = new("0 0 24 24",
            """<path d="M12 2.5 c0.9 4.6 2.9 6.6 7.5 7.5 c-4.6 0.9 -6.6 2.9 -7.5 7.5 c-0.9 -4.6 -2.9 -6.6 -7.5 -7.5 c4.6 -0.9 6.6 -2.9 7.5 -7.5 z" fill="currentColor"></path>"""),
        ["star"] = new("0 0 24 24",
            """<path d="M12 2.8 l2.7 5.7 6.2 0.8 -4.5 4.2 1.1 6.1 -5.5 -2.9 -5.5 2.9 1.1 -6.1 -4.5 -4.2 6.2 -0.8 z" fill="#FFDE00" stroke="#1A1A1A" stroke-width="1.8" stroke-linejoin="round"></path>"""),
        ["note"] = new("0 0 24 24",
            """<ellipse cx="7.5" cy="17.4" rx="3.4" ry="2.7" transform="rotate(-14 7.5 17.4)" fill="currentColor"></ellipse><path d="M10.8 17.4 V4.4 c3.6 0.9 5.6 2.8 5.6 6.2" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"></path>"""),
        ["beats"] = new("0 0 24 24",
            """<ellipse cx="5.8" cy="18.2" rx="2.9" ry="2.3" fill="currentColor"></ellipse><ellipse cx="16.6" cy="16.7" rx="2.9" ry="2.3" fill="currentColor"></ellipse><path d="M8.6 18.2 V7 l10.8 -2.6 V16.7" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"></path>"""),
        ["image"] = new("0 0 34 28",
            """<rect x="1" y="6" width="32" height="21" rx="4" fill="#FFFFFF" stroke="#1A1A1A" stroke-width="2.5"></rect><rect x="10" y="1" width="14" height="6" rx="3" fill="#FFFFFF" stroke="#1A1A1A" stroke-width="2.5"></rect><circle cx="17" cy="16" r="6" fill="#FF90E8" stroke="#1A1A1A" stroke-width="2.5"></circle>"""),
        ["send"] = new("0 0 24 24",
            """<path d="M21.5 2.5 L11 13 M21.5 2.5 L14.8 21 l-3.8 -8 -8 -3.8 z" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"></path>"""),
        ["key"] = new("0 0 24 24",
            """<circle cx="15.5" cy="8.5" r="4.7" fill="#FFDE00" stroke="currentColor" stroke-width="2.2"></circle><path d="M12.1 11.9 L4.2 19.8 M6.8 17.2 l2.6 2.6 M4.2 19.8 l2.2 2.2" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"></path>"""),
        ["globe"] = new("0 0 24 24",
            """<circle cx="12" cy="12" r="8.5" fill="none" stroke="currentColor" stroke-width="2.2"></circle><ellipse cx="12" cy="12" rx="3.8" ry="8.5" fill="none" stroke="currentColor" stroke-width="2"></ellipse><path d="M4 12 h16" stroke="currentColor" stroke-width="2"></path>"""),
    };

    public static HtmlString Render(string name, int size = 20, string? style = null)
    {
        if (!Icons.TryGetValue(name, out var icon)) return new HtmlString("");
        var parts = icon.ViewBox.Split(' ');
        var (vbW, vbH) = (double.Parse(parts[2], CultureInfo.InvariantCulture), double.Parse(parts[3], CultureInfo.InvariantCulture));
        var height = (int)Math.Round(size * vbH / vbW);
        var styleStr = "display:block;flex:none;" + (style ?? "");
        var svg = $"<svg width=\"{size}\" height=\"{height}\" viewBox=\"{icon.ViewBox}\" style=\"{styleStr}\">{icon.Body}</svg>";
        return new HtmlString(svg);
    }

    /// <summary>
    /// 果醬罐品牌 logo（同 portal-web BrandLogo）：hover 罐蓋彈開 / 果醬噴濺由
    /// site.css 的 .oj-logo 動畫驅動，需置於 class 含 oj-logo 的容器內。
    /// </summary>
    public static HtmlString BrandJar(int size = 40)
    {
        var height = (int)Math.Round(size * 54.0 / 48.0);
        return new HtmlString(
            $"<svg width=\"{size}\" height=\"{height}\" viewBox=\"0 0 48 54\" style=\"display:block;overflow:visible;filter:drop-shadow(0 4px 8px rgba(26,26,26,0.25))\">" +
            "<g class=\"oj-splash\">" +
            "<path d=\"M17 8 c-1.5 -5 1 -8.5 3.5 -9.5 c-0.5 3 2 4 1.5 7 c-0.4 2.4 -2.5 3.5 -5 2.5 z\" fill=\"#FF90E8\" stroke=\"#1A1A1A\" stroke-width=\"1.8\"></path>" +
            "<circle cx=\"12\" cy=\"1\" r=\"2.6\" fill=\"#D6479B\" stroke=\"#1A1A1A\" stroke-width=\"1.6\"></circle>" +
            "<circle cx=\"27\" cy=\"-4\" r=\"3.2\" fill=\"#FF90E8\" stroke=\"#1A1A1A\" stroke-width=\"1.8\"></circle>" +
            "<circle cx=\"34\" cy=\"-8\" r=\"1.9\" fill=\"#D6479B\" stroke=\"#1A1A1A\" stroke-width=\"1.5\"></circle>" +
            "<circle cx=\"21\" cy=\"-9\" r=\"1.6\" fill=\"#FF90E8\" stroke=\"#1A1A1A\" stroke-width=\"1.5\"></circle>" +
            "</g>" +
            "<rect x=\"7\" y=\"12\" width=\"34\" height=\"38\" rx=\"11\" fill=\"#FF90E8\" stroke=\"#1A1A1A\" stroke-width=\"2.5\"></rect>" +
            "<path d=\"M12 13 h24 v3 c-2.5 3.5 -5.5 -1 -8.5 2.5 c-3 3.5 -6 -2 -9 1 c-2.8 2.8 -5.5 -0.5 -6.5 -1.5 z\" fill=\"#D6479B\" stroke=\"#1A1A1A\" stroke-width=\"2\"></path>" +
            "<rect x=\"12\" y=\"25\" width=\"24\" height=\"16\" rx=\"5\" fill=\"#FFFFFF\" stroke=\"#1A1A1A\" stroke-width=\"2\"></rect>" +
            "<text x=\"24\" y=\"37.5\" text-anchor=\"middle\" font-family=\"'Space Grotesk','Noto Sans TC',sans-serif\" font-weight=\"700\" font-size=\"12.5\" fill=\"#1A1A1A\">OJ</text>" +
            "<g class=\"oj-lid\"><rect x=\"10\" y=\"3\" width=\"28\" height=\"9\" rx=\"4\" fill=\"#FFDE00\" stroke=\"#1A1A1A\" stroke-width=\"2.5\"></rect></g>" +
            "</svg>");
    }

    /// <summary>「JAM」字樣背後的粉色果醬塗抹（hover 時由 .oj-logo 動畫展開）。</summary>
    public static HtmlString JamSmear()
    {
        return new HtmlString(
            "<svg class=\"oj-smear\" viewBox=\"0 0 78 34\" preserveAspectRatio=\"none\" aria-hidden=\"true\">" +
            "<path d=\"M6 16 c-2 -4 0 -8 4 -8 c3 0 4 -3 8 -3 c5 0 6 -4 11 -3 c4 1 7 -1 12 0 c4 1 8 -2 12 0 c4 2 8 0 11 3 c3 2 8 1 9 5 c1 3 -3 4 -1 7 c2 3 -1 6 -5 6 c-3 0 -5 3 -9 2 c-4 -1 -6 2 -10 1 c-4 -1 -8 2 -12 0 c-3 -2 -8 1 -11 -1 c-3 -2 -7 0 -9 -3 c-2 -2 -6 -1 -7 -4 c-1 -2 1 -4 -3 -2 z\" fill=\"#FF90E8\"></path>" +
            "<circle cx=\"73\" cy=\"7\" r=\"2.4\" fill=\"#FF90E8\"></circle>" +
            "<circle cx=\"4\" cy=\"28\" r=\"1.8\" fill=\"#FF90E8\"></circle>" +
            "<circle cx=\"70\" cy=\"29\" r=\"1.6\" fill=\"#FF90E8\"></circle>" +
            "</svg>");
    }
}
