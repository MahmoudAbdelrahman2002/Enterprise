using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Queries.GetAdminRolesList;

public sealed record GetAdminRolesListQuery : PaginationParams, IRequest<PagedResult<RoleListItemDto>>
{
    public string? SearchTerm { get; init; }
    public bool Descending { get; init; }
    public bool? IsSystem { get; init; }

}
