using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Products.Commands.UploadProductImage;

public sealed class UploadProductImageCommandBoundaryValidator : AbstractValidator<UploadProductImageCommand>
{
    public UploadProductImageCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
