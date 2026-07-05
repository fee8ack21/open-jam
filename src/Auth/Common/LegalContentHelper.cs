using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Auth.Common;

/// <summary>
/// 將法律文件純文字內容渲染為條款畫面的章節 HTML。
/// 慣例：「## 」開頭為章節標題（自動編號 01、02…）、「- 」開頭為列點、其餘行為段落；
/// 文字一律經 HTML encode，市集前端（market-web）以相同慣例解析。
/// </summary>
public static class LegalContentHelper
{
    /// <summary>渲染內容為 .legal-sec 章節結構。</summary>
    public static IHtmlContent Render(string content)
    {
        var enc = HtmlEncoder.Default;
        var sb = new StringBuilder();
        var num = 0;
        var sectionOpen = false;
        var listOpen = false;

        void CloseList()
        {
            if (!listOpen) return;
            sb.Append("</ul>");
            listOpen = false;
        }

        void CloseSection()
        {
            CloseList();
            if (!sectionOpen) return;
            sb.Append("</div>");
            sectionOpen = false;
        }

        void OpenSection()
        {
            if (sectionOpen) return;
            sb.Append("<div class=\"legal-sec\">");
            sectionOpen = true;
        }

        foreach (var raw in content.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith("## "))
            {
                CloseSection();
                OpenSection();
                num++;
                sb.Append("<h4><span class=\"num\">").Append(num.ToString("00")).Append("</span> ")
                  .Append(enc.Encode(line[3..].Trim())).Append("</h4>");
            }
            else if (line.StartsWith("- "))
            {
                OpenSection();
                if (!listOpen)
                {
                    sb.Append("<ul>");
                    listOpen = true;
                }
                sb.Append("<li>").Append(enc.Encode(line[2..].Trim())).Append("</li>");
            }
            else
            {
                CloseList();
                OpenSection();
                sb.Append("<p>").Append(enc.Encode(line)).Append("</p>");
            }
        }

        CloseSection();
        return new HtmlString(sb.ToString());
    }
}
