using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Services.Commands.SetMarketplaceServiceActive;

public sealed class SetMarketplaceServiceActiveCommandValidator : AbstractValidator<SetMarketplaceServiceActiveCommand>
{
    public SetMarketplaceServiceActiveCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
