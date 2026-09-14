using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Commands.AdminChangePassword;

public sealed class AdminChangePasswordCommandValidator : AbstractValidator<AdminChangePasswordCommand>
{
    public AdminChangePasswordCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CurrentPassword).Required(localizer);
        RuleFor(x => x.NewPassword).StrongPassword(localizer);
    }
}
