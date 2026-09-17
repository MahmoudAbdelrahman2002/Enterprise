using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Admin.Services.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Queries.GetAdminServicesList;

public sealed record GetAdminServicesListQuery : PaginationParams, IRequest<PagedResult<MarketplaceServiceDto>>
{
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
}
