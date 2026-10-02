using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Orders;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Orders.Queries.GetAdminOrders;

public sealed class GetAdminOrdersQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminOrdersQuery, PagedResult<OrderListItemDto>>
{
    public async Task<PagedResult<OrderListItemDto>> Handle(
        GetAdminOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var (orders, totalCount) = await unitOfWork.Orders.SearchAsync(
            request.ProviderId,
            request.Status,
            request.From,
            request.To,
            pageNumber,
            request.PageSize,
            cancellationToken);

        var items = orders.Select(order => order.ToListItemDto()).ToList();
        return new PagedResult<OrderListItemDto>(items, totalCount, pageNumber, request.PageSize);
    }
}
