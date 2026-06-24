using OrderService.Data.Entities;

namespace OrderService.Models;

/// <summary>建立訂單（結帳）請求。</summary>
public class CreateOrderRequest
{
    /// <summary>購買者電子信箱。</summary>
    /// <example>buyer@example.com</example>
    public string BuyerEmail { get; set; } = "";

    /// <summary>貨幣代碼（小寫，如 "usd"、"twd"）。</summary>
    /// <example>usd</example>
    public string Currency { get; set; } = "usd";

    /// <summary>訂單項目（至少一筆）。</summary>
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

/// <summary>建立訂單時的單一項目。</summary>
public class CreateOrderItemRequest
{
    /// <summary>商品 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CatalogId { get; set; }

    /// <summary>商品版本 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CatalogVersionId { get; set; }

    /// <summary>商品名稱（下單當下快照）。</summary>
    /// <example>復古 8-bit 音效包</example>
    public string CatalogName { get; set; } = "";

    /// <summary>單價（最低貨幣單位，如 cents）。</summary>
    /// <example>1990</example>
    public long UnitPrice { get; set; }
}

/// <summary>取消訂單請求。</summary>
public class CancelOrderRequest
{
    /// <summary>取消原因（選填）。</summary>
    /// <example>改變心意</example>
    public string? Reason { get; set; }
}

/// <summary>訂單列表查詢請求（分頁採 offset / limit）。</summary>
public class ListOrdersRequest
{
    /// <summary>限定購買者使用者 ID；null 表示不限。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? BuyerUserId { get; set; }

    /// <summary>限定購買者 Email；null 表示不限。</summary>
    /// <example>buyer@example.com</example>
    public string? BuyerEmail { get; set; }

    /// <summary>限定訂單狀態；null 表示不限。</summary>
    /// <example>Completed</example>
    public OrderStatus? Status { get; set; }

    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;
}

/// <summary>訂單列表分頁回應。</summary>
public class ListOrdersResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>42</example>
    public int TotalCount { get; set; }

    /// <summary>本頁訂單清單。</summary>
    public List<OrderSummaryDto> Items { get; set; } = [];
}

/// <summary>訂單列表項目（精簡）。</summary>
public class OrderSummaryDto
{
    /// <summary>訂單 ID。</summary>
    public Guid Id { get; set; }

    /// <summary>人類可讀訂單編號。</summary>
    /// <example>OJ-20260624-1A2B3C4D</example>
    public string OrderNumber { get; set; } = "";

    /// <summary>訂單狀態。</summary>
    /// <example>Completed</example>
    public OrderStatus Status { get; set; }

    /// <summary>貨幣代碼。</summary>
    /// <example>usd</example>
    public string Currency { get; set; } = "";

    /// <summary>訂單總金額（最低貨幣單位）。</summary>
    /// <example>1990</example>
    public long TotalAmount { get; set; }

    /// <summary>建立時間（UTC）。</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>履約完成時間（UTC）；null 表示尚未完成。</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>訂單完整資訊回應（含項目與狀態歷程）。</summary>
public class OrderResponse
{
    /// <summary>訂單 ID。</summary>
    public Guid Id { get; set; }

    /// <summary>人類可讀訂單編號。</summary>
    /// <example>OJ-20260624-1A2B3C4D</example>
    public string OrderNumber { get; set; } = "";

    /// <summary>購買者使用者 ID；null 表示匿名購買。</summary>
    public Guid? BuyerUserId { get; set; }

    /// <summary>購買者電子信箱。</summary>
    /// <example>buyer@example.com</example>
    public string BuyerEmail { get; set; } = "";

    /// <summary>訂單狀態。</summary>
    /// <example>Completed</example>
    public OrderStatus Status { get; set; }

    /// <summary>貨幣代碼。</summary>
    /// <example>usd</example>
    public string Currency { get; set; } = "";

    /// <summary>訂單總金額（最低貨幣單位）。</summary>
    /// <example>1990</example>
    public long TotalAmount { get; set; }

    /// <summary>建立時間（UTC）。</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>最後更新時間（UTC）。</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>履約完成時間（UTC）；null 表示尚未完成。</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>訂單項目。</summary>
    public List<OrderItemResponse> Items { get; set; } = [];

    /// <summary>狀態變更歷程（依時間排序）。</summary>
    public List<OrderStatusHistoryResponse> StatusHistory { get; set; } = [];
}

/// <summary>訂單項目回應。</summary>
public class OrderItemResponse
{
    /// <summary>項目 ID。</summary>
    public Guid Id { get; set; }

    /// <summary>商品 ID。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>商品版本 ID。</summary>
    public Guid CatalogVersionId { get; set; }

    /// <summary>商品名稱（下單快照）。</summary>
    /// <example>復古 8-bit 音效包</example>
    public string CatalogName { get; set; } = "";

    /// <summary>單價（最低貨幣單位）。</summary>
    /// <example>1990</example>
    public long UnitPrice { get; set; }
}

/// <summary>訂單狀態歷程回應。</summary>
public class OrderStatusHistoryResponse
{
    /// <summary>變更前狀態；null 表示訂單建立。</summary>
    /// <example>Pending</example>
    public OrderStatus? OldStatus { get; set; }

    /// <summary>變更後狀態。</summary>
    /// <example>Completed</example>
    public OrderStatus NewStatus { get; set; }

    /// <summary>變更原因。</summary>
    /// <example>Payment succeeded</example>
    public string? Reason { get; set; }

    /// <summary>變更時間（UTC）。</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
