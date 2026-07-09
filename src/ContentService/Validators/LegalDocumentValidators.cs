using ContentService.Models;
using FluentValidation;
using Shared.Web;

namespace ContentService.Validators;

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
