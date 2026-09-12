using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Sku)
            .Required(localizer)
            .MaxLen(localizer, 50)
            .Matches("^[A-Za-z0-9-_]+$")
            .WithMessage(_ => localizer[MessageKeys.Validation.SkuFormat]);

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

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage(_ => localizer[MessageKeys.Validation.GreaterThanOrEqual, 0]);
    }
}
