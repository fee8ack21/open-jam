using FluentValidation;
using Shared.Web;
using StoreService.Models;

namespace StoreService.Validators;

/// <summary>全平台商店列表查詢請求驗證：分頁範圍。</summary>
public class ListStoresRequestValidator : AbstractValidator<ListStoresRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ListStoresRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();
    }
}

/// <summary>更新商店資料請求驗證：商店名稱長度（提供時）。</summary>
public class UpdateStoreRequestValidator : AbstractValidator<UpdateStoreRequest>
{
    /// <summary>建立驗證規則。</summary>
    public UpdateStoreRequestValidator()
    {
        RuleFor(x => x.StoreName!)
            .Must(name => name.Trim().Length is >= 1 and <= 100)
            .When(x => x.StoreName is not null)
            .WithMessage("商店名稱長度須為 1–100 字。");
    }
}

/// <summary>Avatar / Banner 上傳簽章 URL 申請請求驗證：檔名、允許的圖片 MIME 類型與檔案大小。</summary>
public class RequestAssetUploadUrlRequestValidator : AbstractValidator<RequestAssetUploadUrlRequest>
{
    /// <summary>允許的圖片 MIME 類型白名單。</summary>
    public static readonly IReadOnlySet<string> AllowedImageContentTypes = new HashSet<string>
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
    };

    /// <summary>建立驗證規則。</summary>
    public RequestAssetUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("原始檔名為必填。")
            .MaximumLength(255).WithMessage("原始檔名長度不得超過 255 字。");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("MIME 類型為必填。")
            .Must(AllowedImageContentTypes.Contains)
            .WithMessage(x => $"不支援的檔案類型：{x.ContentType}");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("檔案大小須大於 0。");
    }
}
