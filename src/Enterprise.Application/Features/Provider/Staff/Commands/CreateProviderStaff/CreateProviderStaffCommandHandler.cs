using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Staff.Commands.CreateProviderStaff;

public sealed class CreateProviderStaffCommandHandler(
    IStaffManagerService staffManagerService,
    IProviderContext providerContext,
    ILogger<CreateProviderStaffCommandHandler> logger)
    : IRequestHandler<CreateProviderStaffCommand, StaffDetailDto>
{
    public async Task<StaffDetailDto> Handle(
        CreateProviderStaffCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

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

        logger.LogInformation(
            "Created staff {StaffId} with email {Email} for provider {ProviderId}",
            created!.Id,
            created.Email,
            providerId);
        return created;
    }
}
