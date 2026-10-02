using FluentValidation;

namespace Enterprise.Application.Features.Client.Cart.Commands.AddToCartCommand;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(c => c.ProviderId).NotEmpty();
        RuleFor(c => c.ProductId).NotEmpty();
        RuleFor(c => c.Quantity).GreaterThan(0);
    }
}