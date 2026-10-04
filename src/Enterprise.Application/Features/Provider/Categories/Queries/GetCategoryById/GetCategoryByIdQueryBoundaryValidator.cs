using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdQueryBoundaryValidator : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
