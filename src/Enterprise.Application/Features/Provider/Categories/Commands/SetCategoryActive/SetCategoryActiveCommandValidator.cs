using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Categories.Commands.SetCategoryActive;

public sealed class SetCategoryActiveCommandValidator : AbstractValidator<SetCategoryActiveCommand>
{
    public SetCategoryActiveCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
