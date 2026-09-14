using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Commands.AdminResetPassword;

public sealed class AdminResetPasswordCommandValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
        RuleFor(x => x.Otp).OtpCode(localizer);
        RuleFor(x => x.NewPassword).StrongPassword(localizer);
    }
}
