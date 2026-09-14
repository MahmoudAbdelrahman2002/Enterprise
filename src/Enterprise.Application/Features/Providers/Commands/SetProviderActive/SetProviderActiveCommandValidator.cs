using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Providers.Commands.SetProviderActive;

public sealed class SetProviderActiveCommandValidator : AbstractValidator<SetProviderActiveCommand>
{
    public SetProviderActiveCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
