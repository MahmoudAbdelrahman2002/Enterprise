using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderRolesList;

public sealed class GetProviderRolesListQueryHandler(
    IRoleManagerService roleManagerService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetProviderRolesListQuery, PagedResult<RoleListItemDto>>
{
    public async Task<PagedResult<RoleListItemDto>> Handle(
        GetProviderRolesListQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var provider = await unitOfWork.Providers.GetByUserIdAsync(userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Provider), userId);

        return await roleManagerService.GetRolesAsync(
            UserType.Provider,
            provider.Id,
            request,
            request.SearchTerm,
            cancellationToken);
    }
}
