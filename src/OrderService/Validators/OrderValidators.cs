using FluentValidation;
using OrderService.Models;
using Shared.Web;

namespace OrderService.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(r => r.StoreId).NotEmpty().WithMessage("商店 ID 不得為空。");
        RuleFor(r => r.BuyerEmail).NotEmpty().EmailAddress().WithMessage("Email 格式不正確。");
        RuleFor(r => r.Items).NotEmpty().WithMessage("訂單至少須包含一個項目。");
        RuleFor(r => r.Items)
            .Must(items => items.Select(i => i.CatalogId).Distinct().Count() == items.Count)
            .WithMessage("訂單項目的商品不得重複。")
            .When(r => r.Items.Count > 0);
        RuleForEach(r => r.Items).SetValidator(new CreateOrderItemRequestValidator());
    }
}

public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(i => i.CatalogId).NotEmpty().WithMessage("商品 ID 不得為空。");
    }
}

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(r => r.Reason).MaximumLength(500).WithMessage("取消原因最多 500 字元。");
    }
}

public class ListOrdersRequestValidator : AbstractValidator<ListOrdersRequest>
{
    public ListOrdersRequestValidator()
    {
        RuleFor(r => r.Offset).ValidOffset();
        RuleFor(r => r.Limit).ValidLimit();
        RuleFor(r => r.BuyerEmail).EmailAddress().When(r => !string.IsNullOrEmpty(r.BuyerEmail))
            .WithMessage("Email 格式不正確。");
    }
}
