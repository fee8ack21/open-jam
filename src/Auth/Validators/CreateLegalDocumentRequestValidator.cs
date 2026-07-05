using Auth.Models;
using FluentValidation;

namespace Auth.Validators;

/// <summary>建立法律文件草稿請求驗證：類型值域、標題與內容必填長度。</summary>
public class CreateLegalDocumentRequestValidator : AbstractValidator<CreateLegalDocumentRequest>
{
    /// <summary>建立驗證規則。</summary>
    public CreateLegalDocumentRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
    }
}
