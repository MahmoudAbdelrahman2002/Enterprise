using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.Services.Queries.GetClientServiceByIdQuery;

public sealed class GetClientServiceByIdQueryBoundaryValidator : AbstractValidator<GetClientServiceByIdQuery>
{
    public GetClientServiceByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ServiceId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
