using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);

        RuleFor(x => x.Product)
            .NotNull()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);

        RuleFor(x => x.Product.Name)
            .NotNull()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .SetValidator(new LocalizedTextValidator(localizer, 200, englishRequired: true));

        RuleFor(x => x.Product.Description!)
            .SetValidator(new LocalizedTextValidator(localizer, 2000, englishRequired: false))
            .When(x => x.Product.Description is not null);

        RuleFor(x => x.Product.Sku)
            .Required(localizer)
            .MaxLen(localizer, 50)
            .Matches("^[A-Za-z0-9-_]+$")
            .WithMessage(_ => localizer[MessageKeys.Validation.SkuFormat]);

        RuleFor(x => x.Product.Price)
            .GreaterThan(0)
            .WithMessage(_ => localizer[MessageKeys.Validation.GreaterThan, 0]);

        RuleFor(x => x.Product.Status)
            .IsInEnum()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
