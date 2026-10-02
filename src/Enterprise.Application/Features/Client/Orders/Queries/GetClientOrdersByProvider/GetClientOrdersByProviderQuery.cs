using Enterprise.Application.Features.Orders.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrdersByProvider;

public sealed record GetClientOrdersByProviderQuery(Guid ProviderId) : IRequest<IReadOnlyList<OrderListItemDto>>;
