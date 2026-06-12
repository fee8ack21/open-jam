namespace StoreService.Data.Entities;

/// <summary>商店狀態。</summary>
public enum StoreStatus
{
    /// <summary>正常營運。</summary>
    Active,

    /// <summary>遭平台停權，暫不可營運，可解除。</summary>
    Suspended,

    /// <summary>已關閉，終態，不可逆。</summary>
    Closed,
}
