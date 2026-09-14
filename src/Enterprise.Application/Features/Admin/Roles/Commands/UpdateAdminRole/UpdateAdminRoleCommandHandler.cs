using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Commands.UpdateAdminRole;

public sealed class UpdateAdminRoleCommandHandler(
    IRoleManagerService roleManagerService)
    : IRequestHandler<UpdateAdminRoleCommand, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(
        UpdateAdminRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await roleManagerService.UpdateRoleAsync(
            request.Id,
            request.Name,
            UserType.Admin,
            providerId: null,
            request.Permissions,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error == MessageKeys.Error.NotFound)
            {
                throw NotFoundException.For("Role", request.Id);
            }

            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict);
        }

        var updated = await roleManagerService.GetRoleByIdAsync(
            request.Id,
            UserType.Admin,
            providerId: null,
            cancellationToken);

        return updated!;
    }
}
