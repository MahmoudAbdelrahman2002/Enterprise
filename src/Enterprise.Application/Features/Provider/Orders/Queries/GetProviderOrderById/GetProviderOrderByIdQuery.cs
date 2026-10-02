using Enterprise.Application.Features.Orders.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Orders.Queries.GetProviderOrderById;

public sealed record GetProviderOrderByIdQuery(Guid OrderId) : IRequest<OrderDetailDto>;
