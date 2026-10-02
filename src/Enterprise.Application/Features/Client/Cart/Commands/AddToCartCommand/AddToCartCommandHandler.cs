using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.Cart.Queries;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Services;
using MediatR;

namespace Enterprise.Application.Features.Client.Cart.Commands.AddToCartCommand;

public sealed class AddToCartCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService, ICurrentCulture currentCulture) : IRequestHandler<AddToCartCommand, ShoppingCartItemDto>
{
    public async Task<ShoppingCartItemDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var languageCode = currentCulture.LanguageCode;
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var product = await unitOfWork.Products.FirstOrDefaultAsync(
                new ProductByIdSpecification(request.ProductId),
                cancellationToken)
            ?? throw NotFoundException.For(nameof(Product), request.ProductId);

        if (product.Status != ProductStatus.Active
            || product.Category is null
            || !product.Category.IsActive
            || product.Category.ProviderId != request.ProviderId)
        {
            throw NotFoundException.For(nameof(Product), request.ProductId);
        }
        var cart = await unitOfWork.Carts.GetCartByProviderIdAndUserIdAsync(
            request.ProviderId, userId, cancellationToken);

        if (cart is null)
        {
            cart = new ShoppingCart
            {
                ProviderId = request.ProviderId,
                UserId = userId
            };
            unitOfWork.Carts.Add(cart);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {


             existingItem = new ShoppingCartItem
            {
                ShoppingCartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };
            cart.Items.Add(existingItem);

        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return existingItem.ToDto(languageCode);
    }
}
