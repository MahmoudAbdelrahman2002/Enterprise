using Enterprise.Application.Features.Client.product;
using MediatR;

namespace Enterprise.Application.Features.Client.product.Queries.GetClientProductByCategory;

public sealed record GetClientProductByCategoryQuery(Guid CategoryId, Guid ProductId)
    : IRequest<ClientProductDto>;
