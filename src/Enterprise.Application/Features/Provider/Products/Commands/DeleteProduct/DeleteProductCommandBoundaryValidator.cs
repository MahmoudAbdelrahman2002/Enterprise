using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandBoundaryValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
