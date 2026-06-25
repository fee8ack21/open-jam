using Auth.Models;
using FluentValidation;
using Shared.Web;

namespace Auth.Validators;

/// <summary>使用者列表查詢請求驗證：分頁範圍。</summary>
public class ListUsersRequestValidator : AbstractValidator<ListUsersRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ListUsersRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();
    }
}
