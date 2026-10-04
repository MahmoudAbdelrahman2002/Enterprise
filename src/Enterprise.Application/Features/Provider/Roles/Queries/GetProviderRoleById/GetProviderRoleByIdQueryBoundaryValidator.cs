using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderRoleById;

public sealed class GetProviderRoleByIdQueryBoundaryValidator : AbstractValidator<GetProviderRoleByIdQuery>
{
    public GetProviderRoleByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
