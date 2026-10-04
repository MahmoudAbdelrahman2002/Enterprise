using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Providers.Queries.GetAdminProviderCategories;

public sealed class GetAdminProviderCategoriesQueryBoundaryValidator : AbstractValidator<GetAdminProviderCategoriesQuery>
{
    public GetAdminProviderCategoriesQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
