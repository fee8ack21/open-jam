using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Auth.Data.Entities;
using Auth.Models;

namespace Auth.Services.Content;

/// <summary>
/// 呼叫 ContentService 公開端點取得啟用中法律文件的 HTTP client。
/// 僅使用匿名端點 <c>GET /v1/legal-documents/active</c>，不需 service token。
/// </summary>
public class ContentServiceClient(IHttpClientFactory httpClientFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// 取得目前啟用中的法律文件；type 為 null 時回傳所有類型的啟用版本。
    /// ContentService 無法連線時擲出例外（由呼叫端決定 fail-closed / fail-open）。
    /// </summary>
    public async Task<List<LegalDocumentDto>> GetActiveAsync(LegalDocumentType? type = null, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("content");

        var url = "v1/legal-documents/active";
        if (type.HasValue)
            url += $"?type={type.Value}";

        var docs = await client.GetFromJsonAsync<List<LegalDocumentDto>>(url, JsonOptions, ct);
        return docs ?? [];
    }
}
