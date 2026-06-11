using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using StoreService.Data;
using StoreService.Data.Entities;

namespace StoreService.Services;

/// <summary>商店 slug 格式與唯一性驗證。</summary>
public static partial class StoreSlugValidator
{
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.Ordinal)
    {
        "www", "api", "admin", "app", "store", "mail", "ftp", "blog", "help", "support",
    };

    /// <summary>
    /// 驗證 slug 格式（小寫英數字 + 連字號，3–30 字，不可開頭/結尾為連字號）並檢查是否為保留字。
    /// 格式或保留字錯誤時拋出 <see cref="ValidationException"/>。
    /// </summary>
    public static void ValidateFormat(string slug)
    {
        if (!SlugFormatRegex().IsMatch(slug))
            throw new ValidationException("商店子網域格式錯誤：須為 3–30 字小寫英數字與連字號，且不可開頭/結尾為連字號。");

        if (ReservedSlugs.Contains(slug))
            throw new ValidationException("此商店子網域為保留字，請改用其他名稱。");
    }

    /// <summary>
    /// 檢查 slug 唯一性，範圍為 <c>Stores.StoreSlug</c> ∪ <c>StoreApplications.StoreSlug WHERE Status = Pending</c>。
    /// 已被使用時拋出 <see cref="ValidationException"/>。
    /// </summary>
    public static async Task EnsureUniqueAsync(StoreDbContext db, string slug, CancellationToken ct)
    {
        var usedByStore = await db.Stores.AnyAsync(s => s.StoreSlug == slug, ct);
        if (usedByStore)
            throw new ValidationException("此商店子網域已被使用。");

        var usedByPendingApplication = await db.StoreApplications
            .AnyAsync(a => a.StoreSlug == slug && a.Status == StoreApplicationStatus.Pending, ct);
        if (usedByPendingApplication)
            throw new ValidationException("此商店子網域已有待審核的申請。");
    }

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]{1,28}[a-z0-9])?$")]
    private static partial Regex SlugFormatRegex();
}
