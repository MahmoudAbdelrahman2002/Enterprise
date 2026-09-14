using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Providers.Commands.DeleteProvider;

public sealed class DeleteProviderCommandValidator : AbstractValidator<DeleteProviderCommand>
{
    public DeleteProviderCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
