using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderPermissions;

public sealed class GetProviderPermissionsQueryHandler(
    IAppLocalizer localizer,
    ILogger<GetProviderPermissionsQueryHandler> logger)
    : IRequestHandler<GetProviderPermissionsQuery, IReadOnlyList<PermissionGroupDto>>
{
    public Task<IReadOnlyList<PermissionGroupDto>> Handle(
        GetProviderPermissionsQuery request, CancellationToken cancellationToken)
    {
        var grouped = PermissionLocalizer.Group(PermissionCatalog.ProviderPermissions, localizer);

        logger.LogInformation(
            "Listed {Count} permission groups ({PermissionCount} permissions)",
            grouped.Count,
            PermissionCatalog.ProviderPermissions.Count);
        return Task.FromResult(grouped);
    }
}
