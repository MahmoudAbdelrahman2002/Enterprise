using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Services.Commands.DeleteMarketplaceServiceImage;

public sealed class DeleteMarketplaceServiceImageCommandHandler(
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorage,
    ICurrentCulture currentCulture,
    ILogger<DeleteMarketplaceServiceImageCommandHandler> logger) : IRequestHandler<DeleteMarketplaceServiceImageCommand, MarketplaceServiceDto>
{
    public async Task<MarketplaceServiceDto> Handle(
        DeleteMarketplaceServiceImageCommand request,
        CancellationToken cancellationToken)
    {
        var service = await unitOfWork.Services.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(MarketplaceService), request.Id);

        var previous = service.ImageUrl;
        service.SetImageUrl(null);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try { await fileStorage.DeleteAsync(previous, cancellationToken); }
        catch (Exception ex) { logger.LogWarning(ex, "Best-effort blob delete failed for {Url}", previous); }

        logger.LogInformation("Deleted marketplace service image {ServiceId}", service.Id);
        return service.ToDto(currentCulture.LanguageCode);
    }
}
