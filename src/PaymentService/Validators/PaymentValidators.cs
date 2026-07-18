using FluentValidation;
using PaymentService.Models;
using Shared.Web;

namespace PaymentService.Validators;

public class ListPaymentsRequestValidator : AbstractValidator<ListPaymentsRequest>
{
    public ListPaymentsRequestValidator()
    {
        RuleFor(r => r.Offset).ValidOffset();
        RuleFor(r => r.Limit).ValidLimit();
        RuleFor(r => r.Email).EmailAddress().When(r => !string.IsNullOrEmpty(r.Email))
            .WithMessage("Email 格式不正確。");
        RuleFor(r => r.To).GreaterThanOrEqualTo(r => r.From!.Value)
            .When(r => r.From.HasValue && r.To.HasValue)
            .WithMessage("時間區間上限不得早於下限。");
    }
}

public class CreateCheckoutSessionRequestValidator : AbstractValidator<CreateCheckoutSessionRequest>
{
    public CreateCheckoutSessionRequestValidator()
    {
        RuleFor(r => r.OrderId).NotEmpty().WithMessage("訂單 ID 不得為空。");
        RuleFor(r => r.StoreId).NotEmpty().WithMessage("商店 ID 不得為空。");
        RuleFor(r => r.Email).NotEmpty().EmailAddress().WithMessage("Email 格式不正確。");
        RuleFor(r => r.Amount).GreaterThan(0).WithMessage("金額須大於 0。");
        RuleFor(r => r.Currency).NotEmpty().Length(3).WithMessage("貨幣代碼須為 3 字元。");
        RuleFor(r => r.ProductName).NotEmpty().MaximumLength(200).WithMessage("商品名稱不得為空且最多 200 字元。");
    }
}
