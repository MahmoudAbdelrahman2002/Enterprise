using MediatR;
using Enterprise.Application.Features.Client.Cart.Queries;
namespace Enterprise.Application.Features.Client.Cart.Commands.UpdateCartItemCommand;

public class UpdateCartItemCommand(Guid providerId, Guid cartItemId, int quantity) : IRequest<ShoppingCartItemDto>
{
    public Guid ProviderId { get; init; } = providerId;
    public Guid CartItemId { get; init; } = cartItemId;
    public int Quantity { get; init; } = quantity;
    
}
