using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Roles.Queries.GetAdminPermissions;

public sealed class GetAdminPermissionsQueryHandler(
    IAppLocalizer localizer,
    ILogger<GetAdminPermissionsQueryHandler> logger)
    : IRequestHandler<GetAdminPermissionsQuery, IReadOnlyList<PermissionGroupDto>>
{
    public Task<IReadOnlyList<PermissionGroupDto>> Handle(
        GetAdminPermissionsQuery request, CancellationToken cancellationToken)
    {
        var grouped = PermissionLocalizer.Group(PermissionCatalog.AdminPermissions, localizer);

        logger.LogInformation("Listed {Count} admin permission groups", grouped.Count);
        return Task.FromResult(grouped);
    }
}
