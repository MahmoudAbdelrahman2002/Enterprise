using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Provider.Orders.Commands.UpdateProviderOrderStatus;

public sealed record UpdateProviderOrderStatusCommand(Guid OrderId, OrderStatus Status) : IRequest<OrderDetailDto>;
