using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Orders.Queries.GetProviderOrders;

public sealed class GetProviderOrdersQueryBoundaryValidator : AbstractValidator<GetProviderOrdersQuery>
{
    public GetProviderOrdersQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Status).Must(value => !value.HasValue || Enum.IsDefined(value.Value)).WithMessage(_ => localizer[MessageKeys.Validation.AllowedValue]);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ValidationPolicy.PageSizeMax).WithMessage(_ => localizer[MessageKeys.Validation.PageRange, ValidationPolicy.PageSizeMax]);
        RuleFor(x => x.PageNumber).Must((query, number) => number >= 1 && (long)(number - 1) * query.PageSize <= int.MaxValue).WithMessage(_ => localizer[MessageKeys.Validation.PageRange, ValidationPolicy.PageSizeMax]);
        RuleFor(x => x.To).Must((query, to) => !query.From.HasValue || !to.HasValue || query.From.Value <= to.Value).WithMessage(_ => localizer[MessageKeys.Validation.DateRange]);
    }
}
