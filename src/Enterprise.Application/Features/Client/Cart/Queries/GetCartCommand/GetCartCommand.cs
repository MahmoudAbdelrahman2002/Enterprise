using Enterprise.Application.Features.Client.Cart.Queries;
using MediatR;

namespace Enterprise.Application.Features.Client.Cart.Queries.GetCartCommand;

public sealed class GetCartCommand(Guid providerId) : IRequest<ShoppingCartDto>
{
    public Guid ProviderId { get; init; } = providerId;
}
