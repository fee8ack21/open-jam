using CatalogService.Models;
using FluentValidation;

namespace CatalogService.Validators;

/// <summary>建立商品版本請求驗證：版本字串長度。</summary>
public class CreateCatalogVersionRequestValidator : AbstractValidator<CreateCatalogVersionRequest>
{
    /// <summary>建立驗證規則。</summary>
    public CreateCatalogVersionRequestValidator()
    {
        RuleFor(x => x.Version)
            .Must(v => v.Trim().Length is >= 1 and <= 50)
            .WithMessage("版本字串長度須為 1–50 字。");
    }
}

/// <summary>申請版本可下載檔案上傳簽章 URL 請求驗證。</summary>
public class RequestVersionAssetUploadUrlRequestValidator : AbstractValidator<RequestVersionAssetUploadUrlRequest>
{
    /// <summary>建立驗證規則。</summary>
    public RequestVersionAssetUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("原始檔名為必填。")
            .MaximumLength(255).WithMessage("原始檔名長度不得超過 255 字。");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("MIME 類型為必填。");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("檔案大小須大於 0。");
    }
}
