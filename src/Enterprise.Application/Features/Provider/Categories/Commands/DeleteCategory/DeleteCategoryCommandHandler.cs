using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    ICurrentUserService currentUserService,
    IDateTime dateTime,
    ILogger<DeleteCategoryCommandHandler> logger) : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var category = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.Id, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.Id);

        var productCount = await unitOfWork.Products.CountByCategoryIdAndProviderIdAsync(
            category.Id,
            providerId,
            cancellationToken);

        if (productCount > 0 && !request.DeleteRelatedProducts)
        {
            throw new ConflictException(MessageKeys.Category.HasLinkedProducts, productCount);
        }

        if (productCount > 0)
        {
            var deletedBy = currentUserService.Email ?? "system";
            await unitOfWork.Products.SoftDeleteByCategoryIdAndProviderIdAsync(
                category.Id,
                providerId,
                deletedBy,
                dateTime.UtcNow,
                cancellationToken);
        }

        unitOfWork.Categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deleted category {CategoryId} for provider {ProviderId} with {ProductCount} related products",
            category.Id,
            providerId,
            productCount);
    }
}
