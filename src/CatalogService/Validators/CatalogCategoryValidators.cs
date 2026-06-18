using CatalogService.Models;
using CatalogService.Services;
using FluentValidation;

namespace CatalogService.Validators;

/// <summary>建立商品分類請求驗證：名稱長度與代稱格式。</summary>
public class CreateCatalogCategoryRequestValidator : AbstractValidator<CreateCatalogCategoryRequest>
{
    /// <summary>建立驗證規則。</summary>
    public CreateCatalogCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => name.Trim().Length is >= 1 and <= 100)
            .WithMessage("分類名稱長度須為 1–100 字。");

        RuleFor(x => x.Slug)
            .Must(slug => CatalogSlugValidator.IsValidFormat(CatalogInputRules.NormalizeSlug(slug)))
            .WithMessage(CatalogInputRules.SlugFormatMessage);
    }
}

/// <summary>更新商品分類請求驗證：名稱與代稱於提供時驗證。</summary>
public class UpdateCatalogCategoryRequestValidator : AbstractValidator<UpdateCatalogCategoryRequest>
{
    /// <summary>建立驗證規則。</summary>
    public UpdateCatalogCategoryRequestValidator()
    {
        RuleFor(x => x.Name!)
            .Must(name => name.Trim().Length is >= 1 and <= 100)
            .When(x => x.Name is not null)
            .WithMessage("分類名稱長度須為 1–100 字。");

        RuleFor(x => x.Slug!)
            .Must(slug => CatalogSlugValidator.IsValidFormat(CatalogInputRules.NormalizeSlug(slug)))
            .When(x => x.Slug is not null)
            .WithMessage(CatalogInputRules.SlugFormatMessage);
    }
}
