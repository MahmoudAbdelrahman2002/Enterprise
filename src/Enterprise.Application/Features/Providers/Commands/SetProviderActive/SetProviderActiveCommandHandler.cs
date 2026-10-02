using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Providers.Commands.SetProviderActive;

public sealed class SetProviderActiveCommandHandler(
    IUnitOfWork unitOfWork,
    IUserAccountService userAccountService,
    IProviderAdminQueryService providerAdminQueryService,
    ILogger<SetProviderActiveCommandHandler> logger) : IRequestHandler<SetProviderActiveCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(SetProviderActiveCommand request, CancellationToken cancellationToken)
    {
        var provider = await unitOfWork.Providers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.Entities.Provider), request.Id);

        // Owner account drives marketplace visibility / provider login.
        await userAccountService.SetActiveAsync(provider.UserId, request.IsActive, cancellationToken);

        // When taking a store offline, also lock out active staff for that provider.
        if (!request.IsActive)
        {
            var recipients = await userAccountService.GetActiveProviderRecipientIdsAsync(
                provider.Id,
                cancellationToken);
            foreach (var staffId in recipients.Where(id => id != provider.UserId))
            {
                await userAccountService.SetActiveAsync(staffId, isActive: false, cancellationToken);
            }
        }

        var detail = await providerAdminQueryService.GetByIdAsync(provider.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.Entities.Provider), request.Id);

        logger.LogInformation("Set provider {ProviderId} active={IsActive}", request.Id, request.IsActive);
        return detail.ToDto();
    }
}
