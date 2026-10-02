using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Roles.Queries.GetAdminRolesList;

public sealed class GetAdminRolesListQueryHandler(
    IRoleManagerService roleManagerService,
    ILogger<GetAdminRolesListQueryHandler> logger)
    : IRequestHandler<GetAdminRolesListQuery, PagedResult<RoleListItemDto>>
{
    public async Task<PagedResult<RoleListItemDto>> Handle(
        GetAdminRolesListQuery request, CancellationToken cancellationToken)
    {
        var result = await roleManagerService.GetRolesAsync(
            UserType.Admin,
            providerId: null,
            request,
            request.SearchTerm,
            cancellationToken);

        logger.LogInformation("Listed {Count} admin roles", result.TotalCount);
        return result;
    }
}
