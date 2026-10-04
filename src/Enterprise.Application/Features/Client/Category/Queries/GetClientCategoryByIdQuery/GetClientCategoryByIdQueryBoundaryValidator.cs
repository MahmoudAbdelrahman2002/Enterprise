using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.Category.Queries.GetClientCategoryByIdQuery;

public sealed class GetClientCategoryByIdQueryBoundaryValidator : AbstractValidator<GetClientCategoryByIdQuery>
{
    public GetClientCategoryByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
