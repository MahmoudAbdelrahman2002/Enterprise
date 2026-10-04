using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Roles.Queries.GetAdminRoleById;

public sealed class GetAdminRoleByIdQueryBoundaryValidator : AbstractValidator<GetAdminRoleByIdQuery>
{
    public GetAdminRoleByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
