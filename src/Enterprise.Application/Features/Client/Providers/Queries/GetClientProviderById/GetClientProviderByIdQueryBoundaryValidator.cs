using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.Providers.Queries.GetClientProviderById;

public sealed class GetClientProviderByIdQueryBoundaryValidator : AbstractValidator<GetClientProviderByIdQuery>
{
    public GetClientProviderByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
