using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Commands.CreateAdminRole;

public sealed class CreateAdminRoleCommandHandler(
    IRoleManagerService roleManagerService)
    : IRequestHandler<CreateAdminRoleCommand, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(
        CreateAdminRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await roleManagerService.CreateRoleAsync(
            request.Name,
            UserType.Admin,
            providerId: null,
            request.Permissions,
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict, result.Errors);
        }

        var created = await roleManagerService.GetRoleByIdAsync(
            result.RoleId!.Value,
            UserType.Admin,
            providerId: null,
            cancellationToken);

        return created!;
    }
}
