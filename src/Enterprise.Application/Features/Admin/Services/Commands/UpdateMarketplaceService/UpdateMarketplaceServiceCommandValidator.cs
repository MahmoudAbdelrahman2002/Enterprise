using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Services.Commands.UpdateMarketplaceService;

public sealed class UpdateMarketplaceServiceCommandValidator : AbstractValidator<UpdateMarketplaceServiceCommand>
{
    public UpdateMarketplaceServiceCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);

        RuleFor(x => x.Code)
            .Required(localizer)
            .MaxLen(localizer, 100)
            .Matches("^[A-Za-z0-9-_]+$")
            .WithMessage(_ => localizer[MessageKeys.Validation.SkuFormat]);

        RuleFor(x => x.Name)
            .NotNull()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .SetValidator(new LocalizedTextValidator(localizer, 200, englishRequired: true));

        RuleFor(x => x.Description!)
            .SetValidator(new LocalizedTextValidator(localizer, 2000, englishRequired: false))
            .When(x => x.Description is not null);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage(_ => localizer[MessageKeys.Validation.GreaterThanOrEqual, 0]);
    }
}
