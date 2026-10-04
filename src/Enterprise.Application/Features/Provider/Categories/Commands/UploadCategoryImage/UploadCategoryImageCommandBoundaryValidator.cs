using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Categories.Commands.UploadCategoryImage;

public sealed class UploadCategoryImageCommandBoundaryValidator : AbstractValidator<UploadCategoryImageCommand>
{
    public UploadCategoryImageCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
