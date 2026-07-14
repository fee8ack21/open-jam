using FluentValidation;
using StorageService.Models;

namespace StorageService.Validators;

/// <summary>上傳簽章 URL 申請請求驗證：擁有者、檔名、允許的 MIME 類型與檔案大小。</summary>
public class RequestUploadUrlRequestValidator : AbstractValidator<RequestUploadUrlRequest>
{
    /// <summary>
    /// 公開讀取物件（<c>IsPublic</c>）允許的 MIME 類型白名單。
    /// 公開物件會於瀏覽器直接渲染（商店 Avatar/Banner、商品縮圖、預覽影音），
    /// 故限縮為圖片 / 影片 / PDF；私有下載檔（買家授權後下載的商品內容）不受此限，允許任意類型。
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedPublicContentTypes = new HashSet<string>
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

        // 任何上傳都須有 MIME 類型；長度上限防惡意超長字串。
        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("MIME 類型為必填。")
            .MaximumLength(255).WithMessage("MIME 類型長度不得超過 255 字。");

        // 僅公開（瀏覽器渲染）物件套用格式白名單；私有下載檔允許任意類型（ZIP / 音訊 / 設計檔等）。
        RuleFor(x => x.ContentType)
            .Must(AllowedPublicContentTypes.Contains)
            .When(x => x.IsPublic)
            .WithMessage(x => $"不支援的公開檔案類型：{x.ContentType}");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("檔案大小須大於 0。");
    }
}
