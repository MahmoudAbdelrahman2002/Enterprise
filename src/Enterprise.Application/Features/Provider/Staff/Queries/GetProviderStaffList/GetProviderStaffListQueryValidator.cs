using FluentValidation;

namespace Enterprise.Application.Features.Provider.Staff.Queries.GetProviderStaffList;

public sealed class GetProviderStaffListQueryValidator : AbstractValidator<GetProviderStaffListQuery>
{
    public GetProviderStaffListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
