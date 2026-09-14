using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Commands.ProviderResetPassword;

public sealed class ProviderResetPasswordCommandValidator : AbstractValidator<ProviderResetPasswordCommand>
{
    public ProviderResetPasswordCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
        RuleFor(x => x.Otp).OtpCode(localizer);
        RuleFor(x => x.NewPassword).StrongPassword(localizer);
    }
}
