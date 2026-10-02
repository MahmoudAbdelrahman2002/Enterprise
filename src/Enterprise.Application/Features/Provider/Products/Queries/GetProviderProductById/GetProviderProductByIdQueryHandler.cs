using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Products.Queries.GetProviderProductById;

public sealed class GetProviderProductByIdQueryHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    ICurrentCulture currentCulture,
    ILogger<GetProviderProductByIdQueryHandler> logger) : IRequestHandler<GetProviderProductByIdQuery, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(GetProviderProductByIdQuery request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        _ = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.CategoryId, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        var product = await unitOfWork.Products.GetByIdAndCategoryIdAndProviderIdAsync(
                request.Id,
                request.CategoryId,
                providerId,
                cancellationToken)
            ?? throw NotFoundException.For(nameof(Product), request.Id);

        logger.LogInformation(
            "Fetched product {ProductId} in category {CategoryId} for provider {ProviderId}",
            product.Id,
            request.CategoryId,
            providerId);
        return product.ToDto(currentCulture.LanguageCode);
    }
}
