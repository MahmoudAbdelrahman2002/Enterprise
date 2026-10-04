using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.product.Queries.GetClientProductByCategory;

public sealed class GetClientProductByCategoryQueryBoundaryValidator : AbstractValidator<GetClientProductByCategoryQuery>
{
    public GetClientProductByCategoryQueryBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.ProductId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
