using FluentValidation;

namespace Shared.Web;

/// <summary>
/// 共用的分頁欄位驗證規則（平台一律採 <c>offset</c> / <c>limit</c>）。
/// 供各 service 的查詢請求驗證器重用，確保 <c>Offset</c> / <c>Limit</c> 範圍一致。
/// </summary>
public static class PaginationRules
{
    /// <summary>每頁筆數上限。</summary>
    public const int MaxLimit = 100;

    /// <summary>驗證略過筆數須 &gt;= 0。</summary>
    public static IRuleBuilderOptions<T, int> ValidOffset<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThanOrEqualTo(0).WithMessage("略過筆數不得為負數。");

    /// <summary>驗證每頁筆數須介於 1 與 <see cref="MaxLimit"/> 之間。</summary>
    public static IRuleBuilderOptions<T, int> ValidLimit<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, MaxLimit).WithMessage($"每頁筆數須介於 1 與 {MaxLimit} 之間。");
}
