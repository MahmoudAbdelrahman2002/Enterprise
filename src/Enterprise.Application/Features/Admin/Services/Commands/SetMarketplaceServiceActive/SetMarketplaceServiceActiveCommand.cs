using Enterprise.Application.Features.Admin.Services.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Commands.SetMarketplaceServiceActive;

public sealed record SetMarketplaceServiceActiveCommand(Guid Id, bool IsActive) : IRequest<MarketplaceServiceDto>;
