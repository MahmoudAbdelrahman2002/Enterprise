using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrdersByProvider;

public sealed class GetClientOrdersByProviderQueryBoundaryValidator : AbstractValidator<GetClientOrdersByProviderQuery>
{
    public GetClientOrdersByProviderQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
