using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Staff.Queries.GetProviderStaffList;

public sealed class GetProviderStaffListQueryHandler(
    IStaffManagerService staffManagerService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetProviderStaffListQuery, PagedResult<StaffListItemDto>>
{
    public async Task<PagedResult<StaffListItemDto>> Handle(
        GetProviderStaffListQuery request, CancellationToken cancellationToken)
    {
        var providerId = await ResolveProviderIdAsync(cancellationToken);

        return await staffManagerService.GetStaffListAsync(
            UserType.Provider,
            providerId,
            request,
            request.SearchTerm,
            request.RoleId,
            request.IsActive,
            cancellationToken);
    }

    private async Task<Guid> ResolveProviderIdAsync(CancellationToken cancellationToken)
    {
        if (currentUserService.ProviderId.HasValue)
        {
            return currentUserService.ProviderId.Value;
        }

        var userId = currentUserService.UserId ?? throw new ForbiddenAccessException();
        var provider = await unitOfWork.Providers.GetByUserIdAsync(userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Provider), userId);

        return provider.Id;
    }
}
