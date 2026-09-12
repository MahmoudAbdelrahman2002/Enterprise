using Enterprise.Application.Common.Behaviors;
using Enterprise.Domain.Common;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>, ICacheableQuery
{
    public string CacheKey => $"product:{Id}:{SupportedLanguages.Current}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
