using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Commands.UpdateProviderRole;

public sealed class UpdateProviderRoleCommandHandler(
    IRoleManagerService roleManagerService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProviderRoleCommand, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(
        UpdateProviderRoleCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var provider = await unitOfWork.Providers.GetByUserIdAsync(userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Provider), userId);

        var result = await roleManagerService.UpdateRoleAsync(
            request.Id,
            request.Name,
            UserType.Provider,
            provider.Id,
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
            provider.Id,
            cancellationToken);

        return updated!;
    }
}
