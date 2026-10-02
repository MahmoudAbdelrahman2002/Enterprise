using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Staff.Commands.UpdateProviderStaff;

public sealed class UpdateProviderStaffCommandHandler(
    IStaffManagerService staffManagerService,
    IProviderContext providerContext,
    ILogger<UpdateProviderStaffCommandHandler> logger)
    : IRequestHandler<UpdateProviderStaffCommand, StaffDetailDto>
{
    public async Task<StaffDetailDto> Handle(
        UpdateProviderStaffCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

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

        logger.LogInformation("Updated staff {StaffId} for provider {ProviderId}", request.Id, providerId);
        return updated!;
    }
}
