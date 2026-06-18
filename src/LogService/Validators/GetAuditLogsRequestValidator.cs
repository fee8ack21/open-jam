using FluentValidation;
using LogService.Models;
using Shared.Web;

namespace LogService.Validators;

/// <summary>稽核事件查詢請求驗證：分頁範圍與時間區間。</summary>
public class GetAuditLogsRequestValidator : AbstractValidator<GetAuditLogsRequest>
{
    /// <summary>建立驗證規則。</summary>
    public GetAuditLogsRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("查詢結束時間不得早於起始時間。");
    }
}
