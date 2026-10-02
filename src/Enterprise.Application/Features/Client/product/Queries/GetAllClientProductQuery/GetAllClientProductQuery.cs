using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.product;
using MediatR;

namespace Enterprise.Application.Features.Client.product.Queries.GetAllClientProductQuery;

public sealed record GetAllClientProductQuery : PaginationParams, IRequest<PagedResult<ClientProductDto>>
{
    public required Guid CategoryId { get; init; }
    public string? SearchTerm { get; init; }
}
