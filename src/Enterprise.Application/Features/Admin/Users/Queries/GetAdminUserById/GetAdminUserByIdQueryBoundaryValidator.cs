using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Users.Queries.GetAdminUserById;

public sealed class GetAdminUserByIdQueryBoundaryValidator : AbstractValidator<GetAdminUserByIdQuery>
{
    public GetAdminUserByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
