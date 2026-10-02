using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Providers.Queries.GetProvidersList;

public sealed class GetProvidersListQueryHandler(
    IProviderAdminQueryService providerAdminQueryService,
    ILogger<GetProvidersListQueryHandler> logger)
    : IRequestHandler<GetProvidersListQuery, PagedResult<ProviderAdminListItemDto>>
{
    public async Task<PagedResult<ProviderAdminListItemDto>> Handle(
        GetProvidersListQuery request, CancellationToken cancellationToken)
    {
        var result = await providerAdminQueryService.GetPagedAsync(
            request.SearchTerm,
            request.IsActive,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        logger.LogInformation("Listed {Count} providers", result.TotalCount);
        return result;
    }
}
