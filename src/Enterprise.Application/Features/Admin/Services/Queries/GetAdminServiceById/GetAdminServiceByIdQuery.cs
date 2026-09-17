using Enterprise.Application.Features.Admin.Services.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Queries.GetAdminServiceById;

public sealed record GetAdminServiceByIdQuery(Guid Id) : IRequest<MarketplaceServiceDto>;
