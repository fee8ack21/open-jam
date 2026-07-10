using System.Text.RegularExpressions;
using ContentService.Models;
using FluentValidation;

namespace ContentService.Validators;

/// <summary>常見問題分類 slug 格式規則（小寫英數字 + 連字號，3–100 字，不可開頭/結尾為連字號）。</summary>
internal static partial class FaqCategoryRules
{
    /// <summary>slug 格式錯誤訊息。</summary>
    public const string SlugFormatMessage = "代稱格式錯誤：須為 3–100 字小寫英數字與連字號，且不可開頭/結尾為連字號。";

    /// <summary>正規化 slug（去空白、轉小寫）。</summary>
    public static string NormalizeSlug(string? slug) => (slug ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>slug 是否符合格式。</summary>
    public static bool IsValidSlug(string slug) => SlugFormatRegex().IsMatch(slug);

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]{1,98}[a-z0-9])?$")]
    private static partial Regex SlugFormatRegex();
}

/// <summary>建立常見問題分類請求驗證：名稱長度與代稱格式。</summary>
public class CreateFaqCategoryRequestValidator : AbstractValidator<CreateFaqCategoryRequest>
{
    /// <summary>建立驗證規則。</summary>
    public CreateFaqCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => name.Trim().Length is >= 1 and <= 100)
            .WithMessage("分類名稱長度須為 1–100 字。");

        RuleFor(x => x.Slug)
            .Must(slug => FaqCategoryRules.IsValidSlug(FaqCategoryRules.NormalizeSlug(slug)))
            .WithMessage(FaqCategoryRules.SlugFormatMessage);

        RuleFor(x => x.Description!)
            .MaximumLength(200)
            .When(x => x.Description is not null);
    }
}

/// <summary>更新常見問題分類請求驗證：名稱與代稱於提供時驗證。</summary>
public class UpdateFaqCategoryRequestValidator : AbstractValidator<UpdateFaqCategoryRequest>
{
    /// <summary>建立驗證規則。</summary>
    public UpdateFaqCategoryRequestValidator()
    {
        RuleFor(x => x.Name!)
            .Must(name => name.Trim().Length is >= 1 and <= 100)
            .When(x => x.Name is not null)
            .WithMessage("分類名稱長度須為 1–100 字。");

        RuleFor(x => x.Slug!)
            .Must(slug => FaqCategoryRules.IsValidSlug(FaqCategoryRules.NormalizeSlug(slug)))
            .When(x => x.Slug is not null)
            .WithMessage(FaqCategoryRules.SlugFormatMessage);

        RuleFor(x => x.Description!)
            .MaximumLength(200)
            .When(x => x.Description is not null);
    }
}
