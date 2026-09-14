using Enterprise.Application.Common.Authorization;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderPermissions;

public sealed class GetProviderPermissionsQueryHandler
    : IRequestHandler<GetProviderPermissionsQuery, IReadOnlyList<PermissionGroupDto>>
{
    public Task<IReadOnlyList<PermissionGroupDto>> Handle(
        GetProviderPermissionsQuery request, CancellationToken cancellationToken)
    {
        var providerPermissions = PermissionCatalog.ProviderPermissions;
        var grouped = providerPermissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto(
                g.Key,
                g.Select(p => new PermissionItemDto(p.Name, p.Action, p.Description)).ToList()))
            .ToList();

        return Task.FromResult<IReadOnlyList<PermissionGroupDto>>(grouped);
    }
}
