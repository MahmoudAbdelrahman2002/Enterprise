using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Orders.Queries.GetAdminOrderById;

public sealed class GetAdminOrderByIdQueryBoundaryValidator : AbstractValidator<GetAdminOrderByIdQuery>
{
    public GetAdminOrderByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
