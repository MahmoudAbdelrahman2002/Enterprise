using MediatR;
namespace Enterprise.Application.Features.Client.Cart.Commands.DeleteCartItemCommand;

public class DeleteCartItemCommand(Guid providerId,Guid cartItemId) : IRequest
{
    public Guid ProviderId { get; init; } = providerId;
    public Guid CartItemId { get; init; } = cartItemId;
}
