using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Users.Queries.GetAdminUsersList;

public sealed class GetAdminUsersListQueryHandler(
    IStaffManagerService staffManagerService)
    : IRequestHandler<GetAdminUsersListQuery, PagedResult<StaffListItemDto>>
{
    public Task<PagedResult<StaffListItemDto>> Handle(
        GetAdminUsersListQuery request, CancellationToken cancellationToken) =>
        staffManagerService.GetStaffListAsync(
            UserType.Admin,
            providerId: null,
            request,
            request.SearchTerm,
            request.RoleId,
            request.IsActive,
            cancellationToken);
}
