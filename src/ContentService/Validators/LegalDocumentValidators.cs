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

/// <summary>建立法律文件草稿請求驗證：類型值域、標題與內容必填長度、重點速覽行格式。</summary>
public class CreateLegalDocumentRequestValidator : AbstractValidator<CreateLegalDocumentRequest>
{
    /// <summary>建立驗證規則。</summary>
    public CreateLegalDocumentRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
        RuleFor(x => x.Highlights).ValidHighlights();
    }
}

/// <summary>更新法律文件草稿請求驗證：標題與內容必填長度、重點速覽行格式。</summary>
public class UpdateLegalDocumentRequestValidator : AbstractValidator<UpdateLegalDocumentRequest>
{
    /// <summary>建立驗證規則。</summary>
    public UpdateLegalDocumentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
        RuleFor(x => x.Highlights).ValidHighlights();
    }
}

/// <summary>重點速覽欄位共用驗證規則：總長度、則數與「標題|描述」行格式（欄位可留空）。</summary>
public static class LegalHighlightRules
{
    /// <summary>速覽最多則數。</summary>
    public const int MaxItems = 6;

    /// <summary>套用重點速覽驗證：總長 2000 字元內、最多 6 行、每行以第一個「|」分隔且標題與描述皆非空。</summary>
    public static IRuleBuilderOptions<T, string> ValidHighlights<T>(this IRuleBuilder<T, string> rule) => rule
        .MaximumLength(2000).WithMessage("重點速覽總長度不得超過 2000 字元。")
        .Must(h => Lines(h).Count() <= MaxItems).WithMessage($"重點速覽最多 {MaxItems} 則。")
        .Must(h => Lines(h).All(IsValidLine)).WithMessage("重點速覽每行須為「標題|描述」格式（以第一個「|」分隔，兩側皆不可為空）。");

    private static IEnumerable<string> Lines(string? value) =>
        (value ?? "").Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0);

    private static bool IsValidLine(string line)
    {
        var i = line.IndexOf('|');
        return i > 0 && line[..i].Trim().Length > 0 && line[(i + 1)..].Trim().Length > 0;
    }
}
