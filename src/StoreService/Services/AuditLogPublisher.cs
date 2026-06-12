using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Shared.Events;
using StoreService.Data;
using StoreService.Data.Entities;

namespace StoreService.Services;

/// <summary>將 <see cref="AuditLogRequestedEvent"/> 寫入 Outbox，並自請求上下文補齊 IP／UserAgent／CorrelationId。</summary>
public class AuditLogPublisher(StoreDbContext db, IHttpContextAccessor httpContextAccessor)
{
    /// <summary>在目前 DbContext 追蹤範圍內加入一筆審計日誌 Outbox 訊息（需由呼叫端 SaveChanges）。</summary>
    /// <param name="who">執行操作的使用者 ID。</param>
    /// <param name="action">操作代碼，例如 <c>store.suspend</c>。</param>
    /// <param name="target">目標資源類型，例如 <c>Store</c>、<c>StoreApplication</c>。</param>
    /// <param name="targetId">目標資源 ID。</param>
    /// <param name="tenant">所屬租戶（商店）ID；無對應時為 null。</param>
    public void Add(Guid? who, string action, string target, Guid targetId, Guid? tenant)
    {
        var http = httpContextAccessor.HttpContext;

        var outbox = new OutboxMessage { EventType = "audit." + action };
        outbox.Payload = JsonSerializer.Serialize(new AuditLogRequestedEvent(
            OutboxMessageId: outbox.Id,
            Who:             who,
            Action:          action,
            Target:          target,
            TargetId:        targetId,
            Result:          "success",
            Before:          null,
            After:           null,
            Ip:              http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent:       http?.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null,
            Tenant:          tenant,
            OccurredAt:      DateTimeOffset.UtcNow,
            CorrelationId:   http?.TraceIdentifier));

        db.OutboxMessages.Add(outbox);
    }
}
