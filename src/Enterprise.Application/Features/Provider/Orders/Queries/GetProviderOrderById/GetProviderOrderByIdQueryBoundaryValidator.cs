using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Orders.Queries.GetProviderOrderById;

public sealed class GetProviderOrderByIdQueryBoundaryValidator : AbstractValidator<GetProviderOrderByIdQuery>
{
    public GetProviderOrderByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
