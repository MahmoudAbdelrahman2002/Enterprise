using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Commands.CreateProviderRole;

public sealed class CreateProviderRoleCommandHandler(
    IRoleManagerService roleManagerService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProviderRoleCommand, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(
        CreateProviderRoleCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var provider = await unitOfWork.Providers.GetByUserIdAsync(userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Provider), userId);

        var result = await roleManagerService.CreateRoleAsync(
            request.Name,
            UserType.Provider,
            provider.Id,
            request.Permissions,
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict);
        }

        var created = await roleManagerService.GetRoleByIdAsync(
            result.RoleId!.Value,
            UserType.Provider,
            provider.Id,
            cancellationToken);

        return created!;
    }
}
