using Asp.Versioning;
using CatalogService.Models;
using CatalogService.Services.Favorites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

/// <summary>使用者商品收藏（wishlist）API：收藏 / 取消收藏 / 查詢，皆須登入。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/catalogs")]
[Authorize]
public class CatalogFavoritesController(ICatalogFavoriteService favoriteService) : ControllerBase
{
    /// <summary>查詢目前使用者已收藏的商品 ID 清單。</summary>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("favorites")]
    [ProducesResponseType<CatalogFavoritesResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogFavoritesResponse>> ListMine(CancellationToken ct) =>
        Ok(await favoriteService.ListMineAsync(ct));

    /// <summary>收藏商品。已收藏則 no-op。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Add(Guid id, CancellationToken ct)
    {
        await favoriteService.AddAsync(id, ct);
        return NoContent();
    }

    /// <summary>取消收藏商品。未收藏則 no-op。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        await favoriteService.RemoveAsync(id, ct);
        return NoContent();
    }
}
