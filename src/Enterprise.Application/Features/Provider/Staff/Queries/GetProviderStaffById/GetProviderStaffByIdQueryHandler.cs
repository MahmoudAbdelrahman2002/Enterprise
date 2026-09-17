using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Staff.Queries.GetProviderStaffById;

public sealed class GetProviderStaffByIdQueryHandler(
    IStaffManagerService staffManagerService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetProviderStaffByIdQuery, StaffDetailDto>
{
    public async Task<StaffDetailDto> Handle(
        GetProviderStaffByIdQuery request, CancellationToken cancellationToken)
    {
        var providerId = await ResolveProviderIdAsync(cancellationToken);

        var staff = await staffManagerService.GetStaffByIdAsync(
            request.Id,
            UserType.Provider,
            providerId,
            cancellationToken);

        if (staff is null)
        {
            throw NotFoundException.For("ProviderStaff", request.Id);
        }

        return staff;
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
