using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Providers;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Store.Commands.UpdateProviderStore;

public sealed class UpdateProviderStoreCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    IProviderAdminQueryService providerAdminQueryService)
    : IRequestHandler<UpdateProviderStoreCommand, ProviderStoreDto>
{
    public async Task<ProviderStoreDto> Handle(
        UpdateProviderStoreCommand request,
        CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var provider = await unitOfWork.Providers.GetByIdAsync(providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), providerId);

        provider.UpdateDetails(
            request.CompanyName.Trim(),
            string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            provider.ServiceId);

        unitOfWork.Providers.Update(provider);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await providerAdminQueryService.GetByIdAsync(provider.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), providerId);

        return ProviderStoreDto.From(detail.ToDto());
    }
}
