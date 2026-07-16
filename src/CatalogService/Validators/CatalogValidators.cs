using CatalogService.Data.Entities;
using CatalogService.Models;
using CatalogService.Services;
using FluentValidation;
using Shared.Web;

namespace CatalogService.Validators;

/// <summary>建立商品請求驗證：商店、名稱、代稱格式、售價與幣別。</summary>
public class CreateCatalogRequestValidator : AbstractValidator<CreateCatalogRequest>
{
    /// <summary>建立驗證規則。</summary>
    public CreateCatalogRequestValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("所屬商店 ID 為必填。");

        RuleFor(x => x.Name)
            .Must(name => name.Trim().Length is >= 1 and <= 200)
            .WithMessage("商品名稱長度須為 1–200 字。");

        RuleFor(x => x.Slug)
            .Must(slug => CatalogSlugValidator.IsValidFormat(CatalogInputRules.NormalizeSlug(slug)))
            .WithMessage(CatalogInputRules.SlugFormatMessage);

        RuleFor(x => x.Summary!)
            .MaximumLength(200)
            .When(x => x.Summary is not null)
            .WithMessage(CatalogInputRules.SummaryLengthMessage);

        RuleFor(x => x.CoverHue!.Value)
            .InclusiveBetween(0, 359)
            .When(x => x.CoverHue is not null)
            .WithMessage(CatalogInputRules.CoverHueMessage);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("售價不得為負數。");

        RuleFor(x => x.Currency!)
            .Must(CatalogInputRules.IsValidCurrency)
            .When(x => x.Currency is not null)
            .WithMessage(CatalogInputRules.CurrencyMessage);
    }
}

/// <summary>更新商品請求驗證：各欄位於提供時驗證。</summary>
public class UpdateCatalogRequestValidator : AbstractValidator<UpdateCatalogRequest>
{
    /// <summary>建立驗證規則。</summary>
    public UpdateCatalogRequestValidator()
    {
        RuleFor(x => x.Name!)
            .Must(name => name.Trim().Length is >= 1 and <= 200)
            .When(x => x.Name is not null)
            .WithMessage("商品名稱長度須為 1–200 字。");

        RuleFor(x => x.Slug!)
            .Must(slug => CatalogSlugValidator.IsValidFormat(CatalogInputRules.NormalizeSlug(slug)))
            .When(x => x.Slug is not null)
            .WithMessage(CatalogInputRules.SlugFormatMessage);

        RuleFor(x => x.Summary!)
            .MaximumLength(200)
            .When(x => x.Summary is not null)
            .WithMessage(CatalogInputRules.SummaryLengthMessage);

        RuleFor(x => x.CoverHue!.Value)
            .InclusiveBetween(0, 359)
            .When(x => x.CoverHue is not null)
            .WithMessage(CatalogInputRules.CoverHueMessage);

        RuleFor(x => x.Price!.Value)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Price is not null)
            .WithMessage("售價不得為負數。");

        RuleFor(x => x.Currency!)
            .Must(CatalogInputRules.IsValidCurrency)
            .When(x => x.Currency is not null)
            .WithMessage(CatalogInputRules.CurrencyMessage);
    }
}

/// <summary>申請展示型資產上傳簽章 URL 請求驗證。</summary>
public class RequestCatalogAssetUploadUrlRequestValidator : AbstractValidator<RequestCatalogAssetUploadUrlRequest>
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private const long MaxPreviewMediaBytes = 100 * 1024 * 1024;

    /// <summary>建立驗證規則。</summary>
    public RequestCatalogAssetUploadUrlRequestValidator()
    {
        RuleFor(x => x.Type)
            .Must(t => t != CatalogAssetType.ExternalVideo)
            .WithMessage("外部影片嵌入不經檔案上傳，請改用 external 端點。");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("原始檔名為必填。")
            .MaximumLength(255).WithMessage("原始檔名長度不得超過 255 字。");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("MIME 類型為必填。");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("檔案大小須大於 0。");

        RuleFor(x => x.SizeBytes)
            .LessThanOrEqualTo(MaxImageBytes)
            .When(x => x.Type is CatalogAssetType.Thumbnail or CatalogAssetType.Screenshot)
            .WithMessage("圖片資產大小不得超過 5 MB。");

        RuleFor(x => x.SizeBytes)
            .LessThanOrEqualTo(MaxPreviewMediaBytes)
            .When(x => x.Type is CatalogAssetType.PreviewVideo or CatalogAssetType.PreviewAudio)
            .WithMessage("預覽影音大小不得超過 100 MB。");
    }
}

/// <summary>重排預覽媒體順序請求驗證：資產 ID 清單。</summary>
public class ReorderPreviewMediaRequestValidator : AbstractValidator<ReorderPreviewMediaRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ReorderPreviewMediaRequestValidator()
    {
        RuleForEach(x => x.AssetIds)
            .NotEmpty().WithMessage("資產 ID 不得為空。");
    }
}

/// <summary>加入外部影片嵌入請求驗證：YouTube 網址格式。</summary>
public class AddExternalVideoAssetRequestValidator : AbstractValidator<AddExternalVideoAssetRequest>
{
    /// <summary>建立驗證規則。</summary>
    public AddExternalVideoAssetRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("影片網址為必填。")
            .MaximumLength(500).WithMessage("影片網址長度不得超過 500 字。")
            .Must(url => YouTubeUrl.TryParseVideoId(url, out _))
            .WithMessage("僅支援 YouTube 影片網址（watch?v= / youtu.be / shorts / embed）。");
    }
}

/// <summary>確認展示型資產上傳完成請求驗證。</summary>
public class ConfirmCatalogAssetRequestValidator : AbstractValidator<ConfirmCatalogAssetRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ConfirmCatalogAssetRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("資產類型無效。")
            .Must(t => t != CatalogAssetType.ExternalVideo)
            .WithMessage("外部影片嵌入不經檔案上傳，請改用 external 端點。");
    }
}

/// <summary>商品列表查詢請求驗證：分頁範圍。</summary>
public class ListCatalogsRequestValidator : AbstractValidator<ListCatalogsRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ListCatalogsRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();

        RuleFor(x => x.MinPrice!.Value)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice is not null)
            .WithMessage("售價下限不得為負數。");

        RuleFor(x => x.MaxPrice!.Value)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice is not null)
            .WithMessage("售價上限不得為負數。");

        RuleFor(x => x)
            .Must(x => x.MinPrice is null || x.MaxPrice is null || x.MinPrice <= x.MaxPrice)
            .WithMessage("售價下限不得大於上限。");
    }
}

/// <summary>重排店長精選順序請求驗證：商店與商品 ID 清單。</summary>
public class ReorderStoreFeaturedRequestValidator : AbstractValidator<ReorderStoreFeaturedRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ReorderStoreFeaturedRequestValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("所屬商店 ID 為必填。");

        RuleForEach(x => x.CatalogIds)
            .NotEmpty().WithMessage("商品 ID 不得為空。");
    }
}

/// <summary>新增 / 更新評論請求驗證：評分範圍與留言長度。</summary>
public class UpsertReviewRequestValidator : AbstractValidator<UpsertReviewRequest>
{
    /// <summary>建立驗證規則。</summary>
    public UpsertReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("評分須介於 1–5。");

        RuleFor(x => x.Comment!)
            .MaximumLength(2000)
            .When(x => x.Comment is not null)
            .WithMessage("留言長度不得超過 2000 字。");
    }
}

/// <summary>評論列表查詢請求驗證：分頁範圍。</summary>
public class ListReviewsRequestValidator : AbstractValidator<ListReviewsRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ListReviewsRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();
    }
}

/// <summary>CatalogService 共用的無狀態輸入驗證輔助（與業務層正規化邏輯一致）。</summary>
internal static class CatalogInputRules
{
    /// <summary>代稱格式錯誤訊息。</summary>
    public const string SlugFormatMessage = "代稱格式錯誤：須為 3–100 字小寫英數字與連字號，且不可開頭/結尾為連字號。";

    /// <summary>幣別格式錯誤訊息。</summary>
    public const string CurrencyMessage = "幣別須為 3 碼英文字母（ISO 4217）。";

    /// <summary>一句話簡介長度錯誤訊息。</summary>
    public const string SummaryLengthMessage = "一句話簡介長度不得超過 200 字。";

    /// <summary>封面色相範圍錯誤訊息。</summary>
    public const string CoverHueMessage = "封面色相須介於 0–359。";

    /// <summary>與業務層一致的 slug 正規化（去空白、轉小寫）。</summary>
    public static string NormalizeSlug(string? slug) => (slug ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>幣別是否為 3 碼英文字母（不分大小寫、容許前後空白）。</summary>
    public static bool IsValidCurrency(string currency)
    {
        var value = currency.Trim();
        return value.Length == 3 && value.All(char.IsLetter);
    }
}
