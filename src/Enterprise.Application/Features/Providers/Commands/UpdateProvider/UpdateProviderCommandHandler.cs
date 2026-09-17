using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Providers.Commands.UpdateProvider;

public sealed class UpdateProviderCommandHandler(
    IUnitOfWork unitOfWork,
    IUserAccountService userAccountService,
    IProviderAdminQueryService providerAdminQueryService) : IRequestHandler<UpdateProviderCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await unitOfWork.Providers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.Entities.Provider), request.Id);

        if (request.ServiceId.HasValue)
        {
            var service = await unitOfWork.Services.GetByIdAsync(request.ServiceId.Value, cancellationToken);
            if (service is null || !service.IsActive)
            {
                throw NotFoundException.For(nameof(MarketplaceService), request.ServiceId.Value);
            }
        }

        provider.UpdateDetails(
            request.CompanyName.Trim(),
            string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            request.ServiceId);

        await userAccountService.UpdateProfileAsync(
            provider.UserId, request.FirstName.Trim(), request.LastName.Trim(), cancellationToken);

        unitOfWork.Providers.Update(provider);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await providerAdminQueryService.GetByIdAsync(provider.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.Entities.Provider), request.Id);

        return detail.ToDto();
    }
}
