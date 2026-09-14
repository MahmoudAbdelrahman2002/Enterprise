using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Providers.Queries.GetProvidersList;

public sealed record GetProvidersListQuery : PaginationParams, IRequest<PagedResult<ProviderAdminListItemDto>>
{
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
}
