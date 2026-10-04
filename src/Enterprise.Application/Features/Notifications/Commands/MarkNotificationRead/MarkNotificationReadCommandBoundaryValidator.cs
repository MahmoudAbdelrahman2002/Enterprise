using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandBoundaryValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.NotificationId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
