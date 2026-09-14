using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Commands.AdminForgotPassword;

public sealed class AdminForgotPasswordCommandValidator : AbstractValidator<AdminForgotPasswordCommand>
{
    public AdminForgotPasswordCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
    }
}
