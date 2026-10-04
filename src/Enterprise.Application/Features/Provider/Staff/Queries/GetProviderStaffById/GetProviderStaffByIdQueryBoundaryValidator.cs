using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Staff.Queries.GetProviderStaffById;

public sealed class GetProviderStaffByIdQueryBoundaryValidator : AbstractValidator<GetProviderStaffByIdQuery>
{
    public GetProviderStaffByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
