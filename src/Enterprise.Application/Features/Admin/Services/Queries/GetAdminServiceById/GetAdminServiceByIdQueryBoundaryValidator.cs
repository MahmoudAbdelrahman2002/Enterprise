using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Services.Queries.GetAdminServiceById;

public sealed class GetAdminServiceByIdQueryBoundaryValidator : AbstractValidator<GetAdminServiceByIdQuery>
{
    public GetAdminServiceByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
