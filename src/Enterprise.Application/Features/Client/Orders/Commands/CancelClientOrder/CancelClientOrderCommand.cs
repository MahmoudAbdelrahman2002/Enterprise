using Enterprise.Application.Features.Orders.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Client.Orders.Commands.CancelClientOrder;

public sealed record CancelClientOrderCommand(Guid OrderId) : IRequest<OrderDetailDto>;
