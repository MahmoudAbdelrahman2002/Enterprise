using Enterprise.Application.Features.Admin.Services.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Queries.GetServicesLookup;

public sealed record GetServicesLookupQuery : IRequest<IReadOnlyList<MarketplaceServiceLookupDto>>;
