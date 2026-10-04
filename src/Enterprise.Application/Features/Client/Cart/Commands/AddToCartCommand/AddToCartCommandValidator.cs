using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.Cart.Commands.AddToCartCommand;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(c => c.ProviderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(c => c.ProductId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(c => c.Quantity).InclusiveBetween(1, ValidationPolicy.QuantityMax).WithMessage(_ => localizer[MessageKeys.Validation.QuantityRange, ValidationPolicy.QuantityMax]);
    }
}
