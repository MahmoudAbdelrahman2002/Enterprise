using FluentValidation;

namespace Enterprise.Application.Features.Notifications.Commands.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
{
    public RegisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Platform).MaximumLength(32).When(x => x.Platform is not null);
    }
}
