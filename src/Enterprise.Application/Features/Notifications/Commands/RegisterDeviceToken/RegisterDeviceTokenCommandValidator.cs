using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Notifications.Commands.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
{
    public RegisterDeviceTokenCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Token).Required(localizer).MaxLen(localizer, ValidationPolicy.DeviceTokenMax)
            .Must(value => !value.Any(char.IsWhiteSpace)).WithMessage(_ => localizer[MessageKeys.Validation.TextFormat]);
        RuleFor(x => x.Platform).Must(value => value is null || new[] { "android", "ios", "web" }.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage(_ => localizer[MessageKeys.Validation.AllowedValue]);
    }
}
