using Enterprise.Application.Common.Behaviors;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductById;

/// <summary>
/// Fetches a product by Id for admin consumers with all translations (en, it, ar).
/// </summary>
public sealed record GetAdminProductByIdQuery(Guid Id) : IRequest<AdminProductDto>, ICacheableQuery
{
    public string CacheKey => $"product:admin:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
