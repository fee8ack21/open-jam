using FluentValidation;
using PaymentService.Models;

namespace PaymentService.Validators;

public class CreateCheckoutSessionRequestValidator : AbstractValidator<CreateCheckoutSessionRequest>
{
    public CreateCheckoutSessionRequestValidator()
    {
        RuleFor(r => r.OrderId).NotEmpty().WithMessage("訂單 ID 不得為空。");
        RuleFor(r => r.Email).NotEmpty().EmailAddress().WithMessage("Email 格式不正確。");
        RuleFor(r => r.Amount).GreaterThan(0).WithMessage("金額須大於 0。");
        RuleFor(r => r.Currency).NotEmpty().Length(3).WithMessage("貨幣代碼須為 3 字元。");
        RuleFor(r => r.ProductName).NotEmpty().MaximumLength(200).WithMessage("商品名稱不得為空且最多 200 字元。");
    }
}
