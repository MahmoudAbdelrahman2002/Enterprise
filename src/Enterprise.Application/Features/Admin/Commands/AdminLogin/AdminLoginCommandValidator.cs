using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Commands.AdminLogin;

public sealed class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
{
    public AdminLoginCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
        RuleFor(x => x.Password).Required(localizer);
    }
}
