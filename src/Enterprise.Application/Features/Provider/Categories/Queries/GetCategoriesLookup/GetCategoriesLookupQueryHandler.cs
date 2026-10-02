using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Categories.Queries.GetCategoriesLookup;

public sealed class GetCategoriesLookupQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture currentCulture,
    IProviderContext providerContext,
    ILogger<GetCategoriesLookupQueryHandler> logger)
    : IRequestHandler<GetCategoriesLookupQuery, IReadOnlyList<CategoryLookupDto>>
{
    public async Task<IReadOnlyList<CategoryLookupDto>> Handle(
        GetCategoriesLookupQuery request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var currentLanguage = currentCulture.LanguageCode;

        var categories = await unitOfWork.Categories.GetByProviderIdAsync(providerId, cancellationToken);

        var result = categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => c.ToLookupDto(currentLanguage))
            .ToList();

        logger.LogInformation("Listed {Count} category lookups for provider {ProviderId}", result.Count, providerId);
        return result;
    }
}
