using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Queries.GetAdminRolesList;

public sealed class GetAdminRolesListQueryHandler(
    IRoleManagerService roleManagerService)
    : IRequestHandler<GetAdminRolesListQuery, PagedResult<RoleListItemDto>>
{
    public Task<PagedResult<RoleListItemDto>> Handle(
        GetAdminRolesListQuery request, CancellationToken cancellationToken) =>
        roleManagerService.GetRolesAsync(
            UserType.Admin,
            providerId: null,
            request,
            request.SearchTerm,
            cancellationToken);
}
