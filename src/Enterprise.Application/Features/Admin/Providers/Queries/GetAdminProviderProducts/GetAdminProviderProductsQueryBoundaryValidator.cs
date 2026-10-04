using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Providers.Queries.GetAdminProviderProducts;

public sealed class GetAdminProviderProductsQueryBoundaryValidator : AbstractValidator<GetAdminProviderProductsQuery>
{
    public GetAdminProviderProductsQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
