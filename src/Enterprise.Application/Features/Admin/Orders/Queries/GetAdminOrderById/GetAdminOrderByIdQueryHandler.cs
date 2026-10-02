using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Features.Orders;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Orders.Queries.GetAdminOrderById;

public sealed class GetAdminOrderByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminOrderByIdQuery, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(GetAdminOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdWithItemsAsync(request.OrderId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Order), request.OrderId);

        return order.ToDetailDto();
    }
}
