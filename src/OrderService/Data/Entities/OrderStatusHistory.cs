using Shared.Audit;

namespace OrderService.Data.Entities;

/// <summary>訂單狀態變更歷程，每次狀態轉移寫入一筆（append-only）。</summary>
public class OrderStatusHistory : ICreatedAt
{
    public Guid Id { get; set; }

    /// <summary>所屬訂單 ID。</summary>
    public Guid OrderId { get; set; }

    /// <summary>變更前狀態；null 表示訂單建立（無前一狀態）。</summary>
    public OrderStatus? OldStatus { get; set; }

    /// <summary>變更後狀態。</summary>
    public OrderStatus NewStatus { get; set; }

    /// <summary>變更原因（如 "Order created"、"Payment succeeded"、買家取消備註）。</summary>
    public string? Reason { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
