using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Users.Queries.GetAdminUserById;

public sealed class GetAdminUserByIdQueryHandler(
    IStaffManagerService staffManagerService,
    ILogger<GetAdminUserByIdQueryHandler> logger)
    : IRequestHandler<GetAdminUserByIdQuery, StaffDetailDto>
{
    public async Task<StaffDetailDto> Handle(
        GetAdminUserByIdQuery request, CancellationToken cancellationToken)
    {
        var staff = await staffManagerService.GetStaffByIdAsync(
            request.Id,
            UserType.Admin,
            providerId: null,
            cancellationToken);

        if (staff is null)
        {
            throw NotFoundException.For("AdminUser", request.Id);
        }

        logger.LogInformation("Fetched admin user {UserId}", request.Id);
        return staff;
    }
}
