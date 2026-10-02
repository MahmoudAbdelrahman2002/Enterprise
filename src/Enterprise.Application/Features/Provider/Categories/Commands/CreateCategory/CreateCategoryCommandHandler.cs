using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    ICurrentCulture currentCulture,
    ILogger<CreateCategoryCommandHandler> logger)
    : IRequestHandler<CreateCategoryCommand, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var category = new Category(
            providerId: providerId,
            displayOrder: request.DisplayOrder,
            isActive: request.IsActive);

        category.ApplyLocalizedContent(request.Name, request.Description);
        unitOfWork.Categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created category {CategoryId} for provider {ProviderId}", category.Id, providerId);
        return category.ToDto(currentCulture.LanguageCode);
    }
}
