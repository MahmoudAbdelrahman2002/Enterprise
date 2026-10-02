using Enterprise.Application.Features.Orders.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrderById;

public sealed record GetClientOrderByIdQuery(Guid OrderId) : IRequest<OrderDetailDto>;
