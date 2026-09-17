using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Provider.Staff.Queries.GetProviderStaffList;

public sealed record GetProviderStaffListQuery : PaginationParams, IRequest<PagedResult<StaffListItemDto>>
{
    public string? SearchTerm { get; init; }
    public Guid? RoleId { get; init; }
    public bool? IsActive { get; init; }
}
