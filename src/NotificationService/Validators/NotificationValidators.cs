using FluentValidation;
using NotificationService.Models;
using Shared.Web;

namespace NotificationService.Validators;

public class ListNotificationsRequestValidator : AbstractValidator<ListNotificationsRequest>
{
    public ListNotificationsRequestValidator()
    {
        RuleFor(r => r.Offset).ValidOffset();
        RuleFor(r => r.Limit).ValidLimit();
    }
}

public class CreateNotificationRequestRequestValidator : AbstractValidator<CreateNotificationRequestRequest>
{
    public CreateNotificationRequestRequestValidator()
    {
        RuleFor(r => r.StoreId).NotEmpty().WithMessage("商店 ID 不得為空。");
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200).WithMessage("標題不得為空且最多 200 字元。");
        RuleFor(r => r.Message).NotEmpty().MaximumLength(4000).WithMessage("內文不得為空且最多 4000 字元。");
    }
}

public class ListNotificationRequestsRequestValidator : AbstractValidator<ListNotificationRequestsRequest>
{
    public ListNotificationRequestsRequestValidator()
    {
        RuleFor(r => r.StoreId).NotEmpty().WithMessage("商店 ID 不得為空。");
        RuleFor(r => r.Offset).ValidOffset();
        RuleFor(r => r.Limit).ValidLimit();
    }
}
