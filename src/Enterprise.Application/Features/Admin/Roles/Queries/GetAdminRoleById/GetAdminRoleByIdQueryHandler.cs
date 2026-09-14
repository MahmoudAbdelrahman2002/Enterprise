using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Queries.GetAdminRoleById;

public sealed class GetAdminRoleByIdQueryHandler(
    IRoleManagerService roleManagerService)
    : IRequestHandler<GetAdminRoleByIdQuery, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(
        GetAdminRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await roleManagerService.GetRoleByIdAsync(
            request.Id,
            UserType.Admin,
            providerId: null,
            cancellationToken);

        if (role is null)
        {
            throw NotFoundException.For("Role", request.Id);
        }

        return role;
    }
}
