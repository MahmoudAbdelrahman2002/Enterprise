using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Providers.Commands.UploadAdminProviderImage;

public sealed class UploadAdminProviderImageCommandBoundaryValidator : AbstractValidator<UploadAdminProviderImageCommand>
{
    public UploadAdminProviderImageCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
