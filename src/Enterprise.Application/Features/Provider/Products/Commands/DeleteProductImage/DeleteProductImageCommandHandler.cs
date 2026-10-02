using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Products.Commands.DeleteProductImage;

public sealed class DeleteProductImageCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    IFileStorageService fileStorage,
    ICurrentCulture currentCulture,
    ILogger<DeleteProductImageCommandHandler> logger) : IRequestHandler<DeleteProductImageCommand, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        _ = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.CategoryId, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        var product = await unitOfWork.Products.GetByIdAndCategoryIdAndProviderIdAsync(
                request.Id, request.CategoryId, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Product), request.Id);

        var previous = product.ImageUrl;
        product.SetImageUrl(null);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try { await fileStorage.DeleteAsync(previous, cancellationToken); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Best-effort blob delete failed for {Url}", previous);
        }

        logger.LogInformation(
            "Deleted image for product {ProductId} for provider {ProviderId}",
            product.Id,
            providerId);
        return product.ToDto(currentCulture.LanguageCode);
    }
}
