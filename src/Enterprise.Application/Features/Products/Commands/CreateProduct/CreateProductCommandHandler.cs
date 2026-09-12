using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(IUnitOfWork unitOfWork, ICurrentCulture culture)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.Products.SkuExistsAsync(request.Sku, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Product.SkuExists, request.Sku);
        }

        var product = new Product(request.Sku, request.Price, request.StockQuantity);
        product.ApplyLocalizedContent(request.Name, request.Description, request.Category);

        unitOfWork.Products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto(culture.LanguageCode);
    }
}
