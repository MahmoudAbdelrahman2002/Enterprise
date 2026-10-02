using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
