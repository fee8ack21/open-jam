using CatalogService.Models;
using FluentValidation;
using Shared.Web;

namespace CatalogService.Validators;

/// <summary>商品標籤查詢請求驗證：分頁範圍。</summary>
public class ListCatalogTagsRequestValidator : AbstractValidator<ListCatalogTagsRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ListCatalogTagsRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();
    }
}
