using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderRolesList;

public sealed class GetProviderRolesListQueryHandler(
    IRoleManagerService roleManagerService,
    IProviderContext providerContext,
    ILogger<GetProviderRolesListQueryHandler> logger)
    : IRequestHandler<GetProviderRolesListQuery, PagedResult<RoleListItemDto>>
{
    public async Task<PagedResult<RoleListItemDto>> Handle(
        GetProviderRolesListQuery request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var result = await roleManagerService.GetRolesAsync(
            UserType.Provider,
            providerId,
            request,
            request.SearchTerm,
            cancellationToken,
            request.Descending, request.IsSystem);

        logger.LogInformation(
            "Listed {Count} roles (total {TotalCount}) for provider {ProviderId}",
            result.Items.Count,
            result.TotalCount,
            providerId);
        return result;
    }
}
