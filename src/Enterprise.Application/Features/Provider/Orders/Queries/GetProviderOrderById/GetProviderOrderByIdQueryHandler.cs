using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Orders;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Orders.Queries.GetProviderOrderById;

public sealed class GetProviderOrderByIdQueryHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext) : IRequestHandler<GetProviderOrderByIdQuery, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(GetProviderOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var order = await unitOfWork.Orders.GetByIdForProviderAsync(request.OrderId, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Order), request.OrderId);

        return order.ToDetailDto();
    }
}
