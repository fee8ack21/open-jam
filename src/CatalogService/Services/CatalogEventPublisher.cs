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
}
