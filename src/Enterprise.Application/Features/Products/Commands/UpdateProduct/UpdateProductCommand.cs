using Enterprise.Application.Common.Behaviors;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    LocalizedText Name,
    LocalizedText? Description,
    LocalizedText Category,
    decimal Price) : IRequest<ProductDto>, ICacheInvalidatorCommand
{
    public IReadOnlyCollection<string> CacheKeyPrefixesToInvalidate =>
        [$"product:{Id}", "products:list"];
}
