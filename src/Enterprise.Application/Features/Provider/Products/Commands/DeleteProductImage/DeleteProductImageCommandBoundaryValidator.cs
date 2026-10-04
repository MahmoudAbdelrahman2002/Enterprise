using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Products.Commands.DeleteProductImage;

public sealed class DeleteProductImageCommandBoundaryValidator : AbstractValidator<DeleteProductImageCommand>
{
    public DeleteProductImageCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
