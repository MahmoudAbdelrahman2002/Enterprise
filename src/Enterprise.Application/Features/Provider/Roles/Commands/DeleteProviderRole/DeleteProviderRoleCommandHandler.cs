using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Roles.Commands.DeleteProviderRole;

public sealed class DeleteProviderRoleCommandHandler(
    IRoleManagerService roleManagerService,
    IProviderContext providerContext,
    ILogger<DeleteProviderRoleCommandHandler> logger)
    : IRequestHandler<DeleteProviderRoleCommand>
{
    public async Task Handle(DeleteProviderRoleCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var result = await roleManagerService.DeleteRoleAsync(
            request.Id,
            UserType.Provider,
            providerId,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error == MessageKeys.Error.NotFound)
            {
                throw NotFoundException.For("Role", request.Id);
            }

            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict, result.Errors);
        }

        logger.LogInformation("Deleted role {RoleId} for provider {ProviderId}", request.Id, providerId);
    }
}
