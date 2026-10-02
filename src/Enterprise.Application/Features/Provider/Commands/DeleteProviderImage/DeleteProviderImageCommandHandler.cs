using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Providers;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Commands.DeleteProviderImage;

public sealed class DeleteProviderImageCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    IFileStorageService fileStorage,
    IProviderAdminQueryService providerAdminQuery,
    ILogger<DeleteProviderImageCommandHandler> logger) : IRequestHandler<DeleteProviderImageCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(DeleteProviderImageCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var provider = await unitOfWork.Providers.GetByIdAsync(providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), providerId);

        var previous = provider.ImageUrl;
        provider.SetImageUrl(null);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try { await fileStorage.DeleteAsync(previous, cancellationToken); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Best-effort blob delete failed for {Url}", previous);
        }

        var detail = await providerAdminQuery.GetByIdAsync(provider.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), provider.Id);

        logger.LogInformation("Deleted provider image for provider {ProviderId}", providerId);
        return detail.ToDto();
    }
}
