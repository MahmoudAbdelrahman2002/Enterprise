using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.Category;
using MediatR;

namespace Enterprise.Application.Features.Client.Category.Queries.GetAllClientCategoryQuery;

public sealed record GetAllClientCategoryQuery : PaginationParams, IRequest<PagedResult<ClientCategoryDto>>
{
    public required Guid ProviderId { get; init; }
    public string? SearchTerm { get; init; }
}
