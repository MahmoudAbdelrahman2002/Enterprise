using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.AdjustStock;

public sealed class AdjustStockCommandHandler(IUnitOfWork unitOfWork, ICurrentCulture culture)
    : IRequestHandler<AdjustStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var product = await unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.Entities.Product), request.ProductId);

        if (request.Delta > 0)
        {
            product.IncreaseStock(request.Delta);
        }
        else
        {
            product.DecreaseStock(-request.Delta);
        }

        unitOfWork.Products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto(culture.LanguageCode);
    }
}
