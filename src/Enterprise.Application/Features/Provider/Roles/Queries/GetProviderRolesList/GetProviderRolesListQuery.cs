using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderRolesList;

public sealed record GetProviderRolesListQuery : PaginationParams, IRequest<PagedResult<RoleListItemDto>>
{
    public string? SearchTerm { get; init; }
    public bool Descending { get; init; }
    public bool? IsSystem { get; init; }

}
