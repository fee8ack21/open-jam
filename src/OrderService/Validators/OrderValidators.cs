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
        RuleFor(r => r.Currency).NotEmpty().Length(3).WithMessage("貨幣代碼須為 3 字元。");
        RuleFor(r => r.Items).NotEmpty().WithMessage("訂單至少須包含一個項目。");
        RuleForEach(r => r.Items).SetValidator(new CreateOrderItemRequestValidator());
    }
}

public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(i => i.CatalogId).NotEmpty().WithMessage("商品 ID 不得為空。");
        RuleFor(i => i.CatalogVersionId).NotEmpty().WithMessage("商品版本 ID 不得為空。");
        RuleFor(i => i.CatalogName).NotEmpty().MaximumLength(200).WithMessage("商品名稱不得為空且最多 200 字元。");
        RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("單價不得為負數。");
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
