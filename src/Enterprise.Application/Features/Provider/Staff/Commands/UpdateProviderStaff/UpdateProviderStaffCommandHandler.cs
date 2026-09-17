using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Staff.Commands.UpdateProviderStaff;

public sealed class UpdateProviderStaffCommandHandler(
    IStaffManagerService staffManagerService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProviderStaffCommand, StaffDetailDto>
{
    public async Task<StaffDetailDto> Handle(
        UpdateProviderStaffCommand request, CancellationToken cancellationToken)
    {
        var providerId = await ResolveProviderIdAsync(cancellationToken);

        var result = await staffManagerService.UpdateStaffAsync(
            request.Id,
            new UpdateStaffRequest(
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.RoleId),
            UserType.Provider,
            providerId,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error == MessageKeys.Error.NotFound)
            {
                throw NotFoundException.For("ProviderStaff", request.Id);
            }

            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict, result.Errors);
        }

        var updated = await staffManagerService.GetStaffByIdAsync(
            request.Id,
            UserType.Provider,
            providerId,
            cancellationToken);

        return updated!;
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
