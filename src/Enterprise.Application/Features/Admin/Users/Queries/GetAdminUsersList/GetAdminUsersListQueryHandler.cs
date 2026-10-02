using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Users.Queries.GetAdminUsersList;

public sealed class GetAdminUsersListQueryHandler(
    IStaffManagerService staffManagerService,
    ILogger<GetAdminUsersListQueryHandler> logger)
    : IRequestHandler<GetAdminUsersListQuery, PagedResult<StaffListItemDto>>
{
    public async Task<PagedResult<StaffListItemDto>> Handle(
        GetAdminUsersListQuery request, CancellationToken cancellationToken)
    {
        var result = await staffManagerService.GetStaffListAsync(
            UserType.Admin,
            providerId: null,
            request,
            request.SearchTerm,
            request.RoleId,
            request.IsActive,
            cancellationToken);

        logger.LogInformation("Listed {Count} admin users", result.TotalCount);
        return result;
    }
}
