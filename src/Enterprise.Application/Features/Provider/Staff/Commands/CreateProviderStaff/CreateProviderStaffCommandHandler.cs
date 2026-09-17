using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Staff.Commands.CreateProviderStaff;

public sealed class CreateProviderStaffCommandHandler(
    IStaffManagerService staffManagerService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProviderStaffCommand, StaffDetailDto>
{
    public async Task<StaffDetailDto> Handle(
        CreateProviderStaffCommand request, CancellationToken cancellationToken)
    {
        var providerId = await ResolveProviderIdAsync(cancellationToken);

        var result = await staffManagerService.CreateStaffAsync(
            new CreateStaffRequest(
                request.FirstName,
                request.LastName,
                request.Email,
                request.PhoneNumber,
                request.Password,
                request.RoleId),
            UserType.Provider,
            providerId,
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict, result.Errors);
        }

        var created = await staffManagerService.GetStaffByIdAsync(
            result.StaffId!.Value,
            UserType.Provider,
            providerId,
            cancellationToken);

        return created!;
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
