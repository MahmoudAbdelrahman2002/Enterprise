using Enterprise.Application.Common.Behaviors;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.AdjustStock;

/// <summary>
/// Positive <see cref="Delta"/> increases stock, negative decreases it. Kept as a single
/// command rather than separate Increase/Decrease commands because both paths share every
/// step except which domain method they call - and it mirrors how a real inventory adjustment
/// (receiving stock vs. correcting a count) is usually modeled as one signed quantity.
/// </summary>
public sealed record AdjustStockCommand(Guid ProductId, int Delta) : IRequest<ProductDto>, ICacheInvalidatorCommand
{
    public IReadOnlyCollection<string> CacheKeyPrefixesToInvalidate => [$"product:{ProductId}", "products:list"];
}
