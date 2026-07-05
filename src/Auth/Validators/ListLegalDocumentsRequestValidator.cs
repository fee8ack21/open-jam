using Auth.Models;
using FluentValidation;
using Shared.Web;

namespace Auth.Validators;

/// <summary>法律文件列表查詢請求驗證：分頁範圍與 enum 值域。</summary>
public class ListLegalDocumentsRequestValidator : AbstractValidator<ListLegalDocumentsRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ListLegalDocumentsRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}
