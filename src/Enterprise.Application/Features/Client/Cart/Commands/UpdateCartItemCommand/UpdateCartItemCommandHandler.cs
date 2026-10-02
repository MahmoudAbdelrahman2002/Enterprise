using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.Cart.Queries;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Cart.Commands.UpdateCartItemCommand;

public sealed class UpdateCartItemCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService, ICurrentCulture currentCulture) : IRequestHandler<UpdateCartItemCommand, ShoppingCartItemDto>
{
    public async Task<ShoppingCartItemDto> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var languageCode = currentCulture.LanguageCode;
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var cart = await unitOfWork.Carts.GetCartByProviderIdAndUserIdAsync(
            request.ProviderId, userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ShoppingCart), request.ProviderId);

        var item = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId)
            ?? throw NotFoundException.For(nameof(ShoppingCartItem), request.CartItemId);

        item.Quantity = request.Quantity;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return item.ToDto(languageCode);
    }
}
