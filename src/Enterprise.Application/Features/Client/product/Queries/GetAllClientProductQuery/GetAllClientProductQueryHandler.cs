using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.product;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Client.product.Queries.GetAllClientProductQuery;

public sealed class GetAllClientProductQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture currentCulture,
    IClientProviderQueryService clientProviderQueryService,
    ILogger<GetAllClientProductQueryHandler> logger)
    : IRequestHandler<GetAllClientProductQuery, PagedResult<ClientProductDto>>
{
    public async Task<PagedResult<ClientProductDto>> Handle(
        GetAllClientProductQuery request, CancellationToken cancellationToken)
    {
        var language = currentCulture.LanguageCode;
        logger.LogInformation("Getting client products for category {CategoryId}", request.CategoryId);

        var category = await unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
        {
            throw NotFoundException.For(nameof(Category), request.CategoryId);
        }

        _ = await clientProviderQueryService.GetByIdAsync(category.ProviderId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        const ProductStatus status = ProductStatus.Active;
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

        var countSpec = ProductFilterSpecification.ForCount(
            request.CategoryId, request.SearchTerm, status, language);
        var totalCount = await unitOfWork.Products.CountAsync(countSpec, cancellationToken);

        var pageSpec = ProductFilterSpecification.ForPage(
            request.CategoryId,
            request.SearchTerm,
            status,
            pageNumber,
            request.PageSize,
            language);
        var products = await unitOfWork.Products.ListAsync(pageSpec, cancellationToken);
        var items = products.Select(p => p.ProductToDto(language)).ToList();

        return new PagedResult<ClientProductDto>(items, totalCount, pageNumber, request.PageSize);
    }
}
