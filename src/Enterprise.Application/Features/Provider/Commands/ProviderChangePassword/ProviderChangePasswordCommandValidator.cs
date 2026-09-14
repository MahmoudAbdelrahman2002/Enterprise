using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Commands.ProviderChangePassword;

public sealed class ProviderChangePasswordCommandValidator : AbstractValidator<ProviderChangePasswordCommand>
{
    public ProviderChangePasswordCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CurrentPassword).Required(localizer);
        RuleFor(x => x.NewPassword).StrongPassword(localizer);
    }
}
