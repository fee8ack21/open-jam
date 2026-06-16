using Shared.Audit;

namespace CatalogService.Data.Entities;

/// <summary>商品版本。每次釋出新版內容即新增一筆，於同一商品內版本字串唯一。</summary>
public class CatalogVersion : ICreatedAt
{
    /// <summary>版本唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>所屬商品 ID。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>版本字串（例如 1.0.0），於同一商品內唯一。</summary>
    public string Version { get; set; } = "";

    /// <summary>版本說明 / 更新紀錄。</summary>
    public string? ReleaseNote { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
