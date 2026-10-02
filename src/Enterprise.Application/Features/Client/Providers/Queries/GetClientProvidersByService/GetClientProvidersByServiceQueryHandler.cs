using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Providers.Queries.GetClientProvidersByService;

public sealed class GetClientProvidersByServiceQueryHandler(
    IUnitOfWork unitOfWork,
    IClientProviderQueryService clientProviderQueryService)
    : IRequestHandler<GetClientProvidersByServiceQuery, PagedResult<ClientProviderListItemDto>>
{
    public async Task<PagedResult<ClientProviderListItemDto>> Handle(
        GetClientProvidersByServiceQuery request,
        CancellationToken cancellationToken)
    {
        var service = await unitOfWork.Services.GetByIdAsync(request.ServiceId, cancellationToken);
        if (service is null || !service.IsActive)
        {
            throw NotFoundException.For(nameof(MarketplaceService), request.ServiceId);
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        return await clientProviderQueryService.GetPagedByServiceIdAsync(
            request.ServiceId,
            request.SearchTerm,
            pageNumber,
            request.PageSize,
            cancellationToken);
    }
}
