using Enterprise.Application.Common.Behaviors;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Sku,
    LocalizedText Name,
    LocalizedText? Description,
    LocalizedText Category,
    decimal Price,
    int StockQuantity) : IRequest<ProductDto>, ICacheInvalidatorCommand
{
    public IReadOnlyCollection<string> CacheKeyPrefixesToInvalidate { get; } = ["products:list"];
}
