using Enterprise.Application.Common.Authorization;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Queries.GetAdminPermissions;

public sealed class GetAdminPermissionsQueryHandler
    : IRequestHandler<GetAdminPermissionsQuery, IReadOnlyList<PermissionGroupDto>>
{
    public Task<IReadOnlyList<PermissionGroupDto>> Handle(
        GetAdminPermissionsQuery request, CancellationToken cancellationToken)
    {
        var adminPermissions = PermissionCatalog.AdminPermissions;
        var grouped = adminPermissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto(
                g.Key,
                g.Select(p => new PermissionItemDto(p.Name, p.Action, p.Description)).ToList()))
            .ToList();

        return Task.FromResult<IReadOnlyList<PermissionGroupDto>>(grouped);
    }
}
