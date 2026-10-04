using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.Cart.Commands.DeleteCartItemCommand;

public sealed class DeleteCartItemCommandBoundaryValidator : AbstractValidator<DeleteCartItemCommand>
{
    public DeleteCartItemCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.CartItemId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
