using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Services.Commands.DeleteMarketplaceServiceImage;

public sealed class DeleteMarketplaceServiceImageCommandBoundaryValidator : AbstractValidator<DeleteMarketplaceServiceImageCommand>
{
    public DeleteMarketplaceServiceImageCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
