using FluentValidation;
using QuotaService.Models;

namespace QuotaService.Validators;

/// <summary><see cref="ChargeRequest"/> 的無狀態輸入驗證（單檔上限等需設定的檢查留在 service 層）。</summary>
public class ChargeRequestValidator : AbstractValidator<ChargeRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ChargeRequestValidator()
    {
        RuleFor(r => r.ChargeId).NotEmpty().WithMessage("扣量 ID 不得為空。");
        RuleFor(r => r.Size).GreaterThan(0).WithMessage("扣量大小須大於 0。");
    }
}

/// <summary><see cref="ChangeProductCountRequest"/> 的輸入驗證。</summary>
public class ChangeProductCountRequestValidator : AbstractValidator<ChangeProductCountRequest>
{
    /// <summary>建立驗證規則。</summary>
    public ChangeProductCountRequestValidator()
    {
        RuleFor(r => r.Delta).NotEqual(0).WithMessage("增減量不得為 0。");
    }
}
