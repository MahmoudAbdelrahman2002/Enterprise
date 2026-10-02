using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    ICurrentCulture currentCulture,
    ILogger<UpdateProductCommandHandler> logger) : IRequestHandler<UpdateProductCommand, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var category = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.CategoryId, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        if (!category.IsActive)
        {
            throw new ConflictException(MessageKeys.Category.Inactive);
        }

        var product = await unitOfWork.Products.GetByIdAndCategoryIdAndProviderIdAsync(
                request.Id,
                request.CategoryId,
                providerId,
                cancellationToken)
            ?? throw NotFoundException.For(nameof(Product), request.Id);

        if (await unitOfWork.Products.ExistsBySkuAsync(
                request.Product.Sku,
                providerId,
                excludeProductId: product.Id,
                cancellationToken: cancellationToken))
        {
            throw new ConflictException(MessageKeys.Product.SkuExists);
        }

        product.UpdateDetails(request.Product.Sku, request.Product.Price);
        product.SetStatus(request.Product.Status);
        product.ApplyLocalizedContent(request.Product.Name, request.Product.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Updated product {ProductId} in category {CategoryId} for provider {ProviderId}",
            product.Id,
            request.CategoryId,
            providerId);
        return product.ToDto(currentCulture.LanguageCode);
    }
}
