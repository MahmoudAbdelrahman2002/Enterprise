using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Staff.Commands.DeleteProviderStaff;

public sealed class DeleteProviderStaffCommandHandler(
    IStaffManagerService staffManagerService,
    IProviderContext providerContext,
    ILogger<DeleteProviderStaffCommandHandler> logger)
    : IRequestHandler<DeleteProviderStaffCommand>
{
    public async Task Handle(DeleteProviderStaffCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var result = await staffManagerService.DeleteStaffAsync(
            request.Id,
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

        logger.LogInformation("Deleted staff {StaffId} for provider {ProviderId}", request.Id, providerId);
    }
}
