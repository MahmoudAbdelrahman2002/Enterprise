using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    ICurrentCulture currentCulture,
    ILogger<CreateProductCommandHandler> logger) : IRequestHandler<CreateProductCommand, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var category = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.CategoryId, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        if (!category.IsActive)
        {
            throw new ConflictException(MessageKeys.Category.Inactive);
        }

        if (await unitOfWork.Products.ExistsBySkuAsync(request.Product.Sku, providerId, cancellationToken: cancellationToken))
        {
            throw new ConflictException(MessageKeys.Product.SkuExists);
        }

        var product = new Product(
            categoryId: request.CategoryId,
            sku: request.Product.Sku,
            price: request.Product.Price,
            status: request.Product.Status);

        product.ApplyLocalizedContent(request.Product.Name, request.Product.Description);
        unitOfWork.Products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created product {ProductId} for provider {ProviderId}", product.Id, providerId);
        return product.ToDto(currentCulture.LanguageCode);
    }
}
