using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Provider.Orders.Queries.GetProviderOrders;

public sealed record GetProviderOrdersQuery : PaginationParams, IRequest<PagedResult<OrderListItemDto>>
{
    public OrderStatus? Status { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
