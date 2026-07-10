using ContentService.Models;
using FluentValidation;
using Shared.Web;

namespace ContentService.Validators;

/// <summary>常見問題列表查詢請求驗證：分頁範圍與 enum 值域。</summary>
public class ListFaqItemsRequestValidator : AbstractValidator<ListFaqItemsRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ListFaqItemsRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();
        RuleFor(x => x.CategoryId).NotEmpty().When(x => x.CategoryId.HasValue);
    }
}

/// <summary>建立常見問題項目請求驗證：分類必填、問題與解答必填長度。</summary>
public class CreateFaqItemRequestValidator : AbstractValidator<CreateFaqItemRequest>
{
    /// <summary>建立驗證規則。</summary>
    public CreateFaqItemRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("請指定所屬主題分類。");
        RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(10_000);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

/// <summary>更新常見問題項目請求驗證：分類必填、問題與解答必填長度。</summary>
public class UpdateFaqItemRequestValidator : AbstractValidator<UpdateFaqItemRequest>
{
    /// <summary>建立驗證規則。</summary>
    public UpdateFaqItemRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("請指定所屬主題分類。");
        RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(10_000);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
