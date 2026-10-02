using FluentValidation;
namespace Enterprise.Application.Features.Client.Cart.Commands.UpdateCartItemCommand;

public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(c => c.ProviderId).NotEmpty();
        RuleFor(c => c.CartItemId).NotEmpty();
        RuleFor(c => c.Quantity).GreaterThan(0);
    }
}