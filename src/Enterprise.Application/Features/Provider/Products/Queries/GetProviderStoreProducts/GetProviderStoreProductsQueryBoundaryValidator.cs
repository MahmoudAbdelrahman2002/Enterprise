using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Products.Queries.GetProviderStoreProducts;

public sealed class GetProviderStoreProductsQueryBoundaryValidator : AbstractValidator<GetProviderStoreProductsQuery>
{
    public GetProviderStoreProductsQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CategoryId).Must(value => !value.HasValue || value.Value != Guid.Empty).WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.Status).Must(value => !value.HasValue || Enum.IsDefined(value.Value)).WithMessage(_ => localizer[MessageKeys.Validation.AllowedValue]);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ValidationPolicy.PageSizeMax).WithMessage(_ => localizer[MessageKeys.Validation.PageRange, ValidationPolicy.PageSizeMax]);
        RuleFor(x => x.PageNumber).Must((query, number) => number >= 1 && (long)(number - 1) * query.PageSize <= int.MaxValue).WithMessage(_ => localizer[MessageKeys.Validation.PageRange, ValidationPolicy.PageSizeMax]);
        RuleFor(x => x.SearchTerm).MaximumLength(ValidationPolicy.SearchMax).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, ValidationPolicy.SearchMax]).Must(value => FieldFormats.PlainText(value)).WithMessage(_ => localizer[MessageKeys.Validation.TextFormat]);
    }
}
