using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);

        RuleFor(x => x.Product)
            .NotNull()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);

        When(x => x.Product is not null, () =>
        {
            RuleFor(x => x.Product.Name)
                .NotNull()
                .WithMessage(_ => localizer[MessageKeys.Validation.Required])
                .SetValidator(new LocalizedTextValidator(localizer, ValidationPolicy.TitleMax, englishRequired: true));

            RuleFor(x => x.Product.Description!)
                .SetValidator(new LocalizedTextValidator(localizer, ValidationPolicy.DescriptionMax, englishRequired: false))
                .When(x => x.Product.Description is not null);

            RuleFor(x => x.Product.Sku)
                .Required(localizer)
                .MaxLen(localizer, ValidationPolicy.SkuMax)
                .Matches("^[A-Za-z0-9][A-Za-z0-9_-]*$")
                .WithMessage(_ => localizer[MessageKeys.Validation.SkuFormat]);

            RuleFor(x => x.Product.Price)
                .InclusiveBetween(0.01m, ValidationPolicy.PriceMax)
                .WithMessage(_ => localizer[MessageKeys.Validation.PriceRange, ValidationPolicy.PriceMax])
                .Must(price => decimal.Round(price, 2) == price)
                .WithMessage(_ => localizer[MessageKeys.Validation.MoneyPrecision]);

            RuleFor(x => x.Product.Status)
                .IsInEnum()
                .WithMessage(_ => localizer[MessageKeys.Validation.AllowedValue]);
        });
    }
}
