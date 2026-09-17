using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Services.Commands.DeleteMarketplaceService;

public sealed class DeleteMarketplaceServiceCommandValidator : AbstractValidator<DeleteMarketplaceServiceCommand>
{
    public DeleteMarketplaceServiceCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
