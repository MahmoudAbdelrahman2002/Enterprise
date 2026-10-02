using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.Category;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Client.Category.Queries.GetAllClientCategoryQuery;

public sealed class GetAllClientCategoryQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture culture,
    IClientProviderQueryService clientProviderQueryService,
    ILogger<GetAllClientCategoryQueryHandler> logger)
    : IRequestHandler<GetAllClientCategoryQuery, PagedResult<ClientCategoryDto>>
{
    public async Task<PagedResult<ClientCategoryDto>> Handle(
        GetAllClientCategoryQuery request, CancellationToken cancellationToken)
    {
        var language = culture.LanguageCode;
        logger.LogInformation("Getting all client categories for provider {ProviderId}", request.ProviderId);

        _ = await clientProviderQueryService.GetByIdAsync(request.ProviderId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), request.ProviderId);

        const bool isActive = true;
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

        var countSpec = CategoryFilterSpecification.ForCount(
            request.ProviderId, request.SearchTerm, isActive, language);
        var totalCount = await unitOfWork.Categories.CountAsync(countSpec, cancellationToken);

        var pageSpec = CategoryFilterSpecification.ForPage(
            request.ProviderId,
            request.SearchTerm,
            isActive,
            pageNumber,
            request.PageSize,
            language);
        var categories = await unitOfWork.Categories.ListAsync(pageSpec, cancellationToken);
        var items = categories.Select(c => c.ClientCategoryToDto(language)).ToList();
        return new PagedResult<ClientCategoryDto>(items, totalCount, pageNumber, request.PageSize);
    }
}
