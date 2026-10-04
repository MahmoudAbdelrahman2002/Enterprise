using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Notifications.Commands.UnregisterDeviceToken;

public sealed class UnregisterDeviceTokenCommandValidator : AbstractValidator<UnregisterDeviceTokenCommand>
{
    public UnregisterDeviceTokenCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Token).Required(localizer).MaxLen(localizer, ValidationPolicy.DeviceTokenMax)
            .Must(value => !value.Any(char.IsWhiteSpace)).WithMessage(_ => localizer[MessageKeys.Validation.TextFormat]);
    }
}
