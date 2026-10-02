using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Client.Services.Queries.GetAllServicesQuery;

public sealed record GetAllServicesQuery :PaginationParams, IRequest<PagedResult<ClientMarketServiceDto>>
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
