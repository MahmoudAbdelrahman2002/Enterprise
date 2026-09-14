using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderRoleById;

public sealed class GetProviderRoleByIdQueryHandler(
    IRoleManagerService roleManagerService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetProviderRoleByIdQuery, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(
        GetProviderRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var provider = await unitOfWork.Providers.GetByUserIdAsync(userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Provider), userId);

        var role = await roleManagerService.GetRoleByIdAsync(
            request.Id,
            UserType.Provider,
            provider.Id,
            cancellationToken);

        if (role is null)
        {
            throw NotFoundException.For("Role", request.Id);
        }

        return role;
    }
}
