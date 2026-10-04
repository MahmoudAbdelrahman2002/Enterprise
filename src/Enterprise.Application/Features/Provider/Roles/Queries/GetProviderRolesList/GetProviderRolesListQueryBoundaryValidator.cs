using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderRolesList;

public sealed class GetProviderRolesListQueryBoundaryValidator : AbstractValidator<GetProviderRolesListQuery>
{
    public GetProviderRolesListQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, ValidationPolicy.PageSizeMax).WithMessage(_ => localizer[MessageKeys.Validation.PageRange, ValidationPolicy.PageSizeMax]);
        RuleFor(x => x.PageNumber).Must((query, number) => number >= 1 && (long)(number - 1) * query.PageSize <= int.MaxValue).WithMessage(_ => localizer[MessageKeys.Validation.PageRange, ValidationPolicy.PageSizeMax]);
        RuleFor(x => x.SearchTerm).MaximumLength(ValidationPolicy.SearchMax).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, ValidationPolicy.SearchMax]).Must(value => FieldFormats.PlainText(value)).WithMessage(_ => localizer[MessageKeys.Validation.TextFormat]);
    }
}
