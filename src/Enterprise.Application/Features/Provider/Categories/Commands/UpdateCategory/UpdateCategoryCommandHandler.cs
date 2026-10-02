using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture currentCulture,
    IProviderContext providerContext,
    ILogger<UpdateCategoryCommandHandler> logger)
    : IRequestHandler<UpdateCategoryCommand, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var category = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.Id, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.Id);

        category.UpdateDetails(request.DisplayOrder);
        category.ApplyLocalizedContent(request.Name, request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated category {CategoryId} for provider {ProviderId}", category.Id, providerId);
        return category.ToDto(currentCulture.LanguageCode);
    }
}
