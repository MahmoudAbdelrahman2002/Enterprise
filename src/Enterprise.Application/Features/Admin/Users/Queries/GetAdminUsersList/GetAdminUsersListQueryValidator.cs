using FluentValidation;

namespace Enterprise.Application.Features.Admin.Users.Queries.GetAdminUsersList;

public sealed class GetAdminUsersListQueryValidator : AbstractValidator<GetAdminUsersListQuery>
{
    public GetAdminUsersListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
