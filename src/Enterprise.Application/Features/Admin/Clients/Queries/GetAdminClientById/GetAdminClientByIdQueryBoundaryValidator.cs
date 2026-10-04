using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Clients.Queries.GetAdminClientById;

public sealed class GetAdminClientByIdQueryBoundaryValidator : AbstractValidator<GetAdminClientByIdQuery>
{
    public GetAdminClientByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
