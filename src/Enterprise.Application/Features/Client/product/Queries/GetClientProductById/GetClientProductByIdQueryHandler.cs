using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Client.product;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Services;
using MediatR;

namespace Enterprise.Application.Features.Client.product.Queries.GetClientProductById;

public sealed class GetClientProductByIdQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture currentCulture,
    IClientProviderQueryService clientProviderQueryService)
    : IRequestHandler<GetClientProductByIdQuery, ClientProductDto>
{
    public async Task<ClientProductDto> Handle(
        GetClientProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await unitOfWork.Products.FirstOrDefaultAsync(
            new ProductByIdSpecification(request.ProductId),
            cancellationToken);

        if (product is null
            || product.Status != ProductStatus.Active
            || product.Category is null
            || !product.Category.IsActive)
        {
            throw NotFoundException.For(nameof(Product), request.ProductId);
        }

        _ = await clientProviderQueryService.GetByIdAsync(product.Category.ProviderId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Product), request.ProductId);

        return product.ProductToDto(currentCulture.LanguageCode);
    }
}
