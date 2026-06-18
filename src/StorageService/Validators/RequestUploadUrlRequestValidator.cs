using FluentValidation;
using StorageService.Models;

namespace StorageService.Validators;

/// <summary>上傳簽章 URL 申請請求驗證：擁有者、檔名、允許的 MIME 類型與檔案大小。</summary>
public class RequestUploadUrlRequestValidator : AbstractValidator<RequestUploadUrlRequest>
{
    /// <summary>允許上傳的 MIME 類型白名單。</summary>
    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>
    {
        "video/mp4", "video/quicktime", "video/x-msvideo", "video/webm",
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
    };

    /// <summary>建立驗證規則。</summary>
    public RequestUploadUrlRequestValidator()
    {
        RuleFor(x => x.CreatorId)
            .NotEmpty().WithMessage("擁有者 ID 為必填。");

        RuleFor(x => x.OriginalName)
            .NotEmpty().WithMessage("原始檔名為必填。")
            .MaximumLength(255).WithMessage("原始檔名長度不得超過 255 字。");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("MIME 類型為必填。")
            .Must(AllowedContentTypes.Contains)
            .WithMessage(x => $"不支援的檔案類型：{x.ContentType}");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("檔案大小須大於 0。");
    }
}
