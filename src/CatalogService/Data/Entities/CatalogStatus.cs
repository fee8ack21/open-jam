namespace CatalogService.Data.Entities;

/// <summary>商品狀態。</summary>
public enum CatalogStatus
{
    /// <summary>草稿，尚未上架，僅 Owner 可見。</summary>
    Draft,

    /// <summary>已上架，公開可購買。</summary>
    Published,

    /// <summary>已下架封存，Owner 主動收起，不公開但可復原。</summary>
    Archived,

    /// <summary>遭平台停權，強制下架，僅 Admin 可解除。</summary>
    Suspended,
}
