using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Roles.Commands.DeleteAdminRole;

public sealed class DeleteAdminRoleCommandHandler(
    IRoleManagerService roleManagerService,
    ILogger<DeleteAdminRoleCommandHandler> logger)
    : IRequestHandler<DeleteAdminRoleCommand>
{
    public async Task Handle(DeleteAdminRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await roleManagerService.DeleteRoleAsync(
            request.Id,
            UserType.Admin,
            providerId: null,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error == MessageKeys.Error.NotFound)
            {
                throw NotFoundException.For("Role", request.Id);
            }

            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict, result.Errors);
        }

        logger.LogInformation("Deleted admin role {RoleId}", request.Id);
    }
}
