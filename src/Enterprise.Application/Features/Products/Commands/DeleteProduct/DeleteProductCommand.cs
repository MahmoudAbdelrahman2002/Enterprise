using Enterprise.Application.Common.Behaviors;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest, ICacheInvalidatorCommand
{
    public IReadOnlyCollection<string> CacheKeyPrefixesToInvalidate => [$"product:{Id}", "products:list"];
}
