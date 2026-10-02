using Enterprise.Application.Features.Orders.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrders;

public sealed record GetClientOrdersQuery : IRequest<IReadOnlyList<OrderListItemDto>>;
