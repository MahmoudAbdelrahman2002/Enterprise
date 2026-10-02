using Enterprise.Application.Features.Orders.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Orders.Queries.GetAdminOrderById;

public sealed record GetAdminOrderByIdQuery(Guid OrderId) : IRequest<OrderDetailDto>;
