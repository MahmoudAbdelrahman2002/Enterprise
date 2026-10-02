using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    ILogger<DeleteProductCommandHandler> logger) : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
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

        unitOfWork.Products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deleted product {ProductId} in category {CategoryId} for provider {ProviderId}",
            product.Id,
            request.CategoryId,
            providerId);
    }
}
