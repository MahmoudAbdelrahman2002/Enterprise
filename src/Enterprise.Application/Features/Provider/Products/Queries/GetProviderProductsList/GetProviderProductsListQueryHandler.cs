using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Products.Queries.GetProviderProductsList;

public sealed class GetProviderProductsListQueryHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    ICurrentCulture currentCulture,
    ILogger<GetProviderProductsListQueryHandler> logger) : IRequestHandler<GetProviderProductsListQuery, IReadOnlyList<ProductListItemDto>>
{
    public async Task<IReadOnlyList<ProductListItemDto>> Handle(
        GetProviderProductsListQuery request,
        CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        _ = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.CategoryId, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        var products = await unitOfWork.Products.GetAllByCategoryIdAndProviderIdAsync(
            request.CategoryId,
            providerId,
            request.Status,
            cancellationToken);

        var result = products.Select(p => p.ToListItemDto(currentCulture.LanguageCode)).ToList();
        logger.LogInformation(
            "Listed {Count} products in category {CategoryId} for provider {ProviderId}",
            result.Count,
            request.CategoryId,
            providerId);
        return result;
    }
}
