using MediatR;
using Enterprise.Application.Features.Client.Cart.Queries;
namespace Enterprise.Application.Features.Client.Cart.Commands.AddToCartCommand;

public class AddToCartCommand (Guid providerId, Guid productId, int quantity) : IRequest<ShoppingCartItemDto>
{
    public Guid ProviderId { get; init; } = providerId;
    public Guid ProductId { get; init; } = productId;
    public int Quantity { get; init; } = quantity;
}
