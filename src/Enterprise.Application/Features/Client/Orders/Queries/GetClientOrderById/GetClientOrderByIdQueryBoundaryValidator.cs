using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrderById;

public sealed class GetClientOrderByIdQueryBoundaryValidator : AbstractValidator<GetClientOrderByIdQuery>
{
    public GetClientOrderByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
