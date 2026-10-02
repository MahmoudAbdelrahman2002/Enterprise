using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Cart.Commands.DeleteCartItemCommand;

public sealed class DeleteCartItemCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteCartItemCommand>
{
    public async Task Handle(DeleteCartItemCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var cart = await unitOfWork.Carts.GetCartByProviderIdAndUserIdAsync(
            request.ProviderId, userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ShoppingCart), request.ProviderId);

        var cartItem = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId)
            ?? throw NotFoundException.For(nameof(ShoppingCartItem), request.CartItemId);

        cart.Items.Remove(cartItem);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
