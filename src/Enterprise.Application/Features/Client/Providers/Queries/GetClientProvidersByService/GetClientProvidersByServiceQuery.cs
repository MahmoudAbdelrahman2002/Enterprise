using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Client.Providers.Queries.GetClientProvidersByService;

public sealed record GetClientProvidersByServiceQuery : PaginationParams, IRequest<PagedResult<ClientProviderListItemDto>>
{
    public required Guid ServiceId { get; init; }
    public string? SearchTerm { get; init; }
}
