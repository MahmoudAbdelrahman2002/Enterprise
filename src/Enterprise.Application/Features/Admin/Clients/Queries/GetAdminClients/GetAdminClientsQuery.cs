using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Admin.Clients.Queries.GetAdminClients;

public sealed record GetAdminClientsQuery : PaginationParams, IRequest<PagedResult<AdminClientDto>>
{
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
}
