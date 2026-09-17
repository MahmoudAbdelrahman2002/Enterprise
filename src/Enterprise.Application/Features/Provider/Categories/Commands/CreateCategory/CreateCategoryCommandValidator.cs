using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Name)
            .NotNull()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .SetValidator(new LocalizedTextValidator(localizer, 200, englishRequired: true));

        RuleFor(x => x.Description!)
            .SetValidator(new LocalizedTextValidator(localizer, 2000, englishRequired: false))
            .When(x => x.Description is not null);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage(_ => localizer[MessageKeys.Validation.GreaterThanOrEqual, 0]);
    }
}
