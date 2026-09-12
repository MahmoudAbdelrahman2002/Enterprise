using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(IUnitOfWork unitOfWork, ICurrentCulture culture)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.Entities.Product), request.Id);

        product.UpdateDetails(request.Price);
        product.ApplyLocalizedContent(request.Name, request.Description, request.Category);

        unitOfWork.Products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto(culture.LanguageCode);
    }
}
