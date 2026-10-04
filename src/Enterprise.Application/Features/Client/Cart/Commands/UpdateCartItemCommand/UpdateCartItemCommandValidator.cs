using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;
namespace Enterprise.Application.Features.Client.Cart.Commands.UpdateCartItemCommand;

public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(c => c.ProviderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(c => c.CartItemId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(c => c.Quantity).InclusiveBetween(1, ValidationPolicy.QuantityMax).WithMessage(_ => localizer[MessageKeys.Validation.QuantityRange, ValidationPolicy.QuantityMax]);
    }
}
