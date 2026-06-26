using System.Text.Json;

namespace Shared.Events;

/// <summary>
/// Outbox Payload 的 JSON 序列化設定。<b>同一服務的 Publisher 與 Relay 兩端必須一致使用</b>，
/// 否則 positional record 會因屬性命名不符而整包反序列化成預設值（round-trip 失敗）。
/// 採 snake_case 與資料庫欄位風格一致。
/// </summary>
public static class OutboxJson
{
    /// <summary>Outbox Payload 序列化 / 反序列化共用設定。</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
