using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Products.Queries.GetProviderProductsList;

public sealed class GetProviderProductsListQueryBoundaryValidator : AbstractValidator<GetProviderProductsListQuery>
{
    public GetProviderProductsListQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.Status).Must(value => !value.HasValue || Enum.IsDefined(value.Value)).WithMessage(_ => localizer[MessageKeys.Validation.AllowedValue]);
    }
}
