using FluentValidation;
using Shared.Web;
using StoreService.Models;

namespace StoreService.Validators;

/// <summary>追蹤 / 取消追蹤商店請求驗證：信箱格式。</summary>
public class FollowStoreRequestValidator : AbstractValidator<FollowStoreRequest>
{
    /// <summary>建立驗證規則。</summary>
    public FollowStoreRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("信箱為必填。")
            .EmailAddress().WithMessage("信箱格式錯誤。");
    }
}

/// <summary>商店追蹤者查詢請求驗證：分頁範圍。</summary>
public class GetStoreFollowersRequestValidator : AbstractValidator<GetStoreFollowersRequest>
{
    /// <summary>建立驗證規則。</summary>
    public GetStoreFollowersRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();
    }
}
