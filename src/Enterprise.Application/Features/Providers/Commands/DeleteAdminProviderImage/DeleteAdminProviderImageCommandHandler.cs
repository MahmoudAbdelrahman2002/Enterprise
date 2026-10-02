using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Providers;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Providers.Commands.DeleteAdminProviderImage;

public sealed class DeleteAdminProviderImageCommandHandler(
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorage,
    IProviderAdminQueryService providerAdminQuery,
    ILogger<DeleteAdminProviderImageCommandHandler> logger) : IRequestHandler<DeleteAdminProviderImageCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(DeleteAdminProviderImageCommand request, CancellationToken cancellationToken)
    {
        var provider = await unitOfWork.Providers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), request.Id);

        var previous = provider.ImageUrl;
        provider.SetImageUrl(null);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try { await fileStorage.DeleteAsync(previous, cancellationToken); }
        catch (Exception ex) { logger.LogWarning(ex, "Best-effort blob delete failed for {Url}", previous); }

        var detail = await providerAdminQuery.GetByIdAsync(provider.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), provider.Id);

        logger.LogInformation("Deleted admin provider image {ProviderId}", provider.Id);
        return detail.ToDto();
    }
}
