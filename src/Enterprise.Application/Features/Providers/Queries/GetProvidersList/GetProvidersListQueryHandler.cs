using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Providers.Queries.GetProvidersList;

public sealed class GetProvidersListQueryHandler(IProviderAdminQueryService providerAdminQueryService)
    : IRequestHandler<GetProvidersListQuery, PagedResult<ProviderAdminListItemDto>>
{
    public Task<PagedResult<ProviderAdminListItemDto>> Handle(
        GetProvidersListQuery request, CancellationToken cancellationToken) =>
        providerAdminQueryService.GetPagedAsync(
            request.SearchTerm,
            request.IsActive,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
}
