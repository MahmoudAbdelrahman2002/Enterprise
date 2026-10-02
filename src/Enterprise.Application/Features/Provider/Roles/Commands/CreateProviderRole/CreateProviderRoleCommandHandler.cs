using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Roles.Commands.CreateProviderRole;

public sealed class CreateProviderRoleCommandHandler(
    IRoleManagerService roleManagerService,
    IProviderContext providerContext,
    ILogger<CreateProviderRoleCommandHandler> logger)
    : IRequestHandler<CreateProviderRoleCommand, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(
        CreateProviderRoleCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var result = await roleManagerService.CreateRoleAsync(
            request.Name,
            UserType.Provider,
            providerId,
            request.Permissions,
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict, result.Errors);
        }

        var created = await roleManagerService.GetRoleByIdAsync(
            result.RoleId!.Value,
            UserType.Provider,
            providerId,
            cancellationToken);

        logger.LogInformation(
            "Created role {RoleId} with {PermissionCount} permissions for provider {ProviderId}",
            created!.Id,
            created.Permissions.Count,
            providerId);
        return created;
    }
}
