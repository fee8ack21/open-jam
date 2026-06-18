using FluentValidation;
using Shared.Web;
using StoreService.Models;
using StoreService.Services;

namespace StoreService.Validators;

/// <summary>提交開店申請請求驗證：商店名稱長度與子網域格式 / 保留字。</summary>
public class SubmitStoreApplicationRequestValidator : AbstractValidator<SubmitStoreApplicationRequest>
{
    /// <summary>建立驗證規則。</summary>
    public SubmitStoreApplicationRequestValidator()
    {
        RuleFor(x => x.StoreName)
            .Must(name => name.Trim().Length is >= 1 and <= 100)
            .WithMessage("商店名稱長度須為 1–100 字。");

        RuleFor(x => x.StoreSlug)
            .Cascade(CascadeMode.Stop)
            .Must(slug => StoreSlugValidator.IsValidFormat(Normalize(slug)))
            .WithMessage("商店子網域格式錯誤：須為 3–30 字小寫英數字與連字號，且不可開頭/結尾為連字號。")
            .Must(slug => !StoreSlugValidator.IsReserved(Normalize(slug)))
            .WithMessage("此商店子網域為保留字，請改用其他名稱。");
    }

    /// <summary>與業務層一致的 slug 正規化（去空白、轉小寫）。</summary>
    private static string Normalize(string? slug) => (slug ?? string.Empty).Trim().ToLowerInvariant();
}

/// <summary>駁回開店申請請求驗證：審核意見必填且不過長。</summary>
public class RejectStoreApplicationRequestValidator : AbstractValidator<RejectStoreApplicationRequest>
{
    /// <summary>建立驗證規則。</summary>
    public RejectStoreApplicationRequestValidator()
    {
        RuleFor(x => x.ReviewComment)
            .NotEmpty().WithMessage("駁回原因為必填。")
            .MaximumLength(1000).WithMessage("駁回原因長度不得超過 1000 字。");
    }
}

/// <summary>開店申請查詢請求驗證：分頁範圍。</summary>
public class GetStoreApplicationsRequestValidator : AbstractValidator<GetStoreApplicationsRequest>
{
    /// <summary>建立驗證規則。</summary>
    public GetStoreApplicationsRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();
    }
}
