using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.product;
using MediatR;

namespace Enterprise.Application.Features.Client.product.Queries.GetClientProductsByProvider;

public sealed record GetClientProductsByProviderQuery : PaginationParams, IRequest<PagedResult<ClientProductDto>>
{
    public required Guid ProviderId { get; init; }
    public Guid? CategoryId { get; init; }
    public string? SearchTerm { get; init; }
}
