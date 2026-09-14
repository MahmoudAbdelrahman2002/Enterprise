using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Commands.ProviderForgotPassword;

public sealed class ProviderForgotPasswordCommandValidator : AbstractValidator<ProviderForgotPasswordCommand>
{
    public ProviderForgotPasswordCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
    }
}
