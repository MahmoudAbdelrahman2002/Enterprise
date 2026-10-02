using FluentValidation;

namespace Enterprise.Application.Features.Notifications.Commands.UnregisterDeviceToken;

public sealed class UnregisterDeviceTokenCommandValidator : AbstractValidator<UnregisterDeviceTokenCommand>
{
    public UnregisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
    }
}
