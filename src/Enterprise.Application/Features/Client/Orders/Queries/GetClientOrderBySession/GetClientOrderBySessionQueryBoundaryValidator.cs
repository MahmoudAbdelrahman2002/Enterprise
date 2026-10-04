using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrderBySession;

public sealed class GetClientOrderBySessionQueryBoundaryValidator : AbstractValidator<GetClientOrderBySessionQuery>
{
    public GetClientOrderBySessionQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.SessionId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]).MaximumLength(ValidationPolicy.SessionIdMax).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, ValidationPolicy.SessionIdMax]).Matches("^cs_(?:test_|live_)?[A-Za-z0-9]+$").WithMessage(_ => localizer[MessageKeys.Validation.SessionFormat]);
    }
}
