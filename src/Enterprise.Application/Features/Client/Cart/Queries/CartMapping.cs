using Enterprise.Domain.Entities;

namespace Enterprise.Application.Features.Client.Cart.Queries;

public static class CartMapping
{
    public static ShoppingCartDto ToDto(this ShoppingCart cart, string? languageCode = null)
    {
        var itemDtos = cart.Items.Select(i =>
        {
            var (name, _) = i.Product.ResolveContent(languageCode);

            return new ShoppingCartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = name,
                ProductImage = i.Product.ImageUrl ?? string.Empty,
                Quantity = i.Quantity,
                Price = i.Product.Price
            };
        }).ToList();

        return new ShoppingCartDto
        {
            Id = cart.Id,
            ProviderId = cart.ProviderId,
            ProviderName = cart.Provider?.CompanyName ?? string.Empty,
            UserId = cart.UserId,
            Items = itemDtos,
            TotalPrice = itemDtos.Sum(i => i.Price * i.Quantity)
        };
    }
    public static ShoppingCartItemDto ToDto(this ShoppingCartItem item, string? languageCode = null)
    {
        var (name, _) = item.Product.ResolveContent(languageCode);
        return new ShoppingCartItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = name,
            ProductImage = item.Product.ImageUrl ?? string.Empty,
            Quantity = item.Quantity,
            Price = item.Product.Price
        };
    }
}
