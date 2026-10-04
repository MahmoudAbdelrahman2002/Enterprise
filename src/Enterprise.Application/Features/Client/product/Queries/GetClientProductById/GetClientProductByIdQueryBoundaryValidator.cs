using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.product.Queries.GetClientProductById;

public sealed class GetClientProductByIdQueryBoundaryValidator : AbstractValidator<GetClientProductByIdQuery>
{
    public GetClientProductByIdQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
