using Enterprise.Application.Features.Client.product;
using MediatR;

namespace Enterprise.Application.Features.Client.product.Queries.GetClientProductById;

public sealed record GetClientProductByIdQuery(Guid ProductId) : IRequest<ClientProductDto>;
