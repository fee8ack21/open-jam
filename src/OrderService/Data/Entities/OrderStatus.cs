namespace OrderService.Data.Entities;

/// <summary>訂單狀態。數位商品付款成功後即視為履約完成（Completed）。</summary>
public enum OrderStatus
{
    /// <summary>已建立，等待付款。</summary>
    Pending = 0,

    /// <summary>已付款，等待履約（保留供需人工審核的流程使用）。</summary>
    Paid = 1,

    /// <summary>已完成履約（買家可下載內容）。</summary>
    Completed = 2,

    /// <summary>已取消（付款前由買家或系統取消）。</summary>
    Cancelled = 3,

    /// <summary>已退款。</summary>
    Refunded = 4,
}
