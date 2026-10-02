using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Categories.Commands.DeleteCategoryImage;

public sealed class DeleteCategoryImageCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    IFileStorageService fileStorage,
    ICurrentCulture currentCulture,
    ILogger<DeleteCategoryImageCommandHandler> logger) : IRequestHandler<DeleteCategoryImageCommand, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(DeleteCategoryImageCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var category = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.Id, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.Id);

        var previous = category.ImageUrl;
        category.SetImageUrl(null);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await fileStorage.DeleteAsync(previous, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Best-effort blob delete failed for {Url}", previous);
        }

        logger.LogInformation(
            "Deleted image for category {CategoryId} for provider {ProviderId}",
            category.Id,
            providerId);
        return category.ToDto(currentCulture.LanguageCode);
    }
}
