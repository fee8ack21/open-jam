using System.Text.Json;
using CatalogService.Data;
using CatalogService.Data.Entities;
using Shared.Events;

namespace CatalogService.Services;

/// <summary>
/// 將商品領域事件寫入 Outbox（業務 transaction 內），由 <see cref="Background.OutboxRelayService"/> 搬入 RabbitMQ。
/// </summary>
public class CatalogEventPublisher(CatalogDbContext db)
{
    /// <summary>商品上架事件的 Outbox EventType。</summary>
    public const string CatalogPublishedType = "catalog.published";

    /// <summary>寫入一筆 CatalogPublishedEvent。不負責 SaveChanges。</summary>
    /// <param name="catalog">已轉為 Published 狀態的商品。</param>
    /// <param name="isFirstPublish">是否為首次上架（發佈前 PublishedAt 為 null）。</param>
    public void AddCatalogPublished(Catalog catalog, bool isFirstPublish)
    {
        var outbox = new OutboxMessage { EventType = CatalogPublishedType };
        outbox.Payload = JsonSerializer.Serialize(new CatalogPublishedEvent(
            OutboxMessageId: outbox.Id,
            CatalogId: catalog.Id,
            StoreId: catalog.StoreId,
            Name: catalog.Name,
            Slug: catalog.Slug,
            Price: catalog.Price,
            Currency: catalog.Currency,
            PublishedAt: catalog.PublishedAt ?? DateTimeOffset.UtcNow,
            IsFirstPublish: isFirstPublish));
        db.OutboxMessages.Add(outbox);
    }

    /// <summary>商品新版本發布通知事件的 Outbox EventType。</summary>
    public const string CatalogVersionReleasedType = "catalog.version_released";

    /// <summary>寫入一筆 CatalogVersionReleasedEvent（通知既有買家）。不負責 SaveChanges。</summary>
    /// <param name="catalog">版本所屬商品（Published 狀態）。</param>
    /// <param name="version">要通知買家的版本。</param>
    public void AddCatalogVersionReleased(Catalog catalog, CatalogVersion version)
    {
        var outbox = new OutboxMessage { EventType = CatalogVersionReleasedType };
        outbox.Payload = JsonSerializer.Serialize(new CatalogVersionReleasedEvent(
            OutboxMessageId: outbox.Id,
            CatalogId: catalog.Id,
            StoreId: catalog.StoreId,
            CatalogName: catalog.Name,
            CatalogSlug: catalog.Slug,
            VersionId: version.Id,
            Version: version.Version,
            ReleaseNote: version.ReleaseNote,
            ReleasedAt: DateTimeOffset.UtcNow));
        db.OutboxMessages.Add(outbox);
    }
}
