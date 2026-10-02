using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderRoleById;

public sealed class GetProviderRoleByIdQueryHandler(
    IRoleManagerService roleManagerService,
    IProviderContext providerContext,
    ILogger<GetProviderRoleByIdQueryHandler> logger)
    : IRequestHandler<GetProviderRoleByIdQuery, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(
        GetProviderRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var role = await roleManagerService.GetRoleByIdAsync(
            request.Id,
            UserType.Provider,
            providerId,
            cancellationToken);

        if (role is null)
        {
            throw NotFoundException.For("Role", request.Id);
        }

        logger.LogInformation("Fetched role {RoleId} for provider {ProviderId}", role.Id, providerId);
        return role;
    }
}
