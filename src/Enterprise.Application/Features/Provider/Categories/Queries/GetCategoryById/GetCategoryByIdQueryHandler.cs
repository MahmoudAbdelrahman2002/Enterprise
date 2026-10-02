using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture currentCulture,
    IProviderContext providerContext,
    ILogger<GetCategoryByIdQueryHandler> logger)
    : IRequestHandler<GetCategoryByIdQuery, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var currentLanguage = currentCulture.LanguageCode;

        var category = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.Id, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.Id);

        logger.LogInformation("Fetched category {CategoryId} for provider {ProviderId}", category.Id, providerId);
        return category.ToDto(currentLanguage);
    }
}
