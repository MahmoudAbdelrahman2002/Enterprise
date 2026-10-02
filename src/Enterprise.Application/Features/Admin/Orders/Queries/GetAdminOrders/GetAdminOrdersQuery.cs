using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Admin.Orders.Queries.GetAdminOrders;

public sealed record GetAdminOrdersQuery : PaginationParams, IRequest<PagedResult<OrderListItemDto>>
{
    public Guid? ProviderId { get; init; }
    public OrderStatus? Status { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
