using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Commands.ProviderLogin;

public sealed class ProviderLoginCommandValidator : AbstractValidator<ProviderLoginCommand>
{
    public ProviderLoginCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
        RuleFor(x => x.Password).Required(localizer);
    }
}
