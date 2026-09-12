using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);

        RuleFor(x => x.Name)
            .NotNull()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .SetValidator(new LocalizedTextValidator(localizer, 200, englishRequired: true));

        RuleFor(x => x.Description!)
            .SetValidator(new LocalizedTextValidator(localizer, 2000, englishRequired: false))
            .When(x => x.Description is not null);

        RuleFor(x => x.Category)
            .NotNull()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .SetValidator(new LocalizedTextValidator(localizer, 100, englishRequired: true));

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage(_ => localizer[MessageKeys.Validation.GreaterThan, 0]);
    }
}
