using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Roles.Commands.UpdateProviderRole;

public sealed class UpdateProviderRoleCommandHandler(
    IRoleManagerService roleManagerService,
    IProviderContext providerContext,
    ILogger<UpdateProviderRoleCommandHandler> logger)
    : IRequestHandler<UpdateProviderRoleCommand, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(
        UpdateProviderRoleCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var result = await roleManagerService.UpdateRoleAsync(
            request.Id,
            request.Name,
            UserType.Provider,
            providerId,
            request.Permissions,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error == MessageKeys.Error.NotFound)
            {
                throw NotFoundException.For("Role", request.Id);
            }

            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict, result.Errors);
        }

        var updated = await roleManagerService.GetRoleByIdAsync(
            request.Id,
            UserType.Provider,
            providerId,
            cancellationToken);

        logger.LogInformation(
            "Updated role {RoleId} with {PermissionCount} permissions for provider {ProviderId}",
            request.Id,
            updated!.Permissions.Count,
            providerId);
        return updated;
    }
}
