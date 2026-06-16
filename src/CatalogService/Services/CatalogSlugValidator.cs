using System.Text.RegularExpressions;
using CatalogService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace CatalogService.Services;

/// <summary>商品 / 分類 slug 格式與唯一性驗證。</summary>
public static partial class CatalogSlugValidator
{
    /// <summary>
    /// 驗證 slug 格式（小寫英數字 + 連字號，3–100 字，不可開頭/結尾為連字號）。
    /// 格式錯誤時拋出 <see cref="ValidationException"/>。
    /// </summary>
    public static void ValidateFormat(string slug)
    {
        if (!SlugFormatRegex().IsMatch(slug))
            throw new ValidationException("代稱格式錯誤：須為 3–100 字小寫英數字與連字號，且不可開頭/結尾為連字號。");
    }

    /// <summary>檢查商品 slug 於同一商店內唯一（排除指定商品自身）。已被使用時拋出 <see cref="ValidationException"/>。</summary>
    public static async Task EnsureCatalogSlugUniqueAsync(
        CatalogDbContext db, Guid storeId, string slug, Guid? excludeCatalogId, CancellationToken ct)
    {
        var used = await db.Catalogs
            .AnyAsync(c => c.StoreId == storeId && c.Slug == slug && c.Id != excludeCatalogId, ct);

        if (used)
            throw new ValidationException("此商品代稱於商店內已被使用。");
    }

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]{1,98}[a-z0-9])?$")]
    private static partial Regex SlugFormatRegex();
}
