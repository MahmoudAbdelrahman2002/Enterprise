using Enterprise.Application.Features.Provider.Products.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Products.Queries.GetProviderProductById;

public sealed record GetProviderProductByIdQuery(Guid CategoryId, Guid Id) : IRequest<ProductDetailDto>;
