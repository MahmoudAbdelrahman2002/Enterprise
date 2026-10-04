using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Providers.Commands.DeleteAdminProviderImage;

public sealed class DeleteAdminProviderImageCommandBoundaryValidator : AbstractValidator<DeleteAdminProviderImageCommand>
{
    public DeleteAdminProviderImageCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
