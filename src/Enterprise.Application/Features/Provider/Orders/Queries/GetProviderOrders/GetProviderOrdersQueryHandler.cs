using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Orders;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Orders.Queries.GetProviderOrders;

public sealed class GetProviderOrdersQueryHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext) : IRequestHandler<GetProviderOrdersQuery, PagedResult<OrderListItemDto>>
{
    public async Task<PagedResult<OrderListItemDto>> Handle(
        GetProviderOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var (orders, totalCount) = await unitOfWork.Orders.SearchAsync(
            providerId,
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
