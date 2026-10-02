using Enterprise.Application.Features.Client.Cart.Queries;
using MediatR;

namespace Enterprise.Application.Features.Client.Cart.Queries.GetClientCarts;

public sealed record GetClientCartsQuery : IRequest<IReadOnlyList<ShoppingCartDto>>;
