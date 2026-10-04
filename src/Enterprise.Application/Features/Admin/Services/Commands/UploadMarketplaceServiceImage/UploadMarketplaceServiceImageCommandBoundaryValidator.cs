using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Services.Commands.UploadMarketplaceServiceImage;

public sealed class UploadMarketplaceServiceImageCommandBoundaryValidator : AbstractValidator<UploadMarketplaceServiceImageCommand>
{
    public UploadMarketplaceServiceImageCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
