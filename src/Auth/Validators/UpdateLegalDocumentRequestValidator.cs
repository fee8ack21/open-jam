using Auth.Models;
using FluentValidation;

namespace Auth.Validators;

/// <summary>更新法律文件草稿請求驗證：標題與內容必填長度。</summary>
public class UpdateLegalDocumentRequestValidator : AbstractValidator<UpdateLegalDocumentRequest>
{
    /// <summary>建立驗證規則。</summary>
    public UpdateLegalDocumentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
    }
}
