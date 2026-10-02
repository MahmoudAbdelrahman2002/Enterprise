using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Staff.Commands.SetProviderStaffActive;

public sealed class SetProviderStaffActiveCommandHandler(
    IStaffManagerService staffManagerService,
    IProviderContext providerContext,
    ILogger<SetProviderStaffActiveCommandHandler> logger)
    : IRequestHandler<SetProviderStaffActiveCommand>
{
    public async Task Handle(SetProviderStaffActiveCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var result = await staffManagerService.SetStaffActiveAsync(
            request.Id,
            request.IsActive,
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

        logger.LogInformation(
            "Set staff {StaffId} active={IsActive} for provider {ProviderId}",
            request.Id,
            request.IsActive,
            providerId);
    }
}
