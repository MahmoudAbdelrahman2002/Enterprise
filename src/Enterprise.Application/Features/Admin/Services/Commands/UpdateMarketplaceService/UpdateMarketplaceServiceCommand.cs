using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Admin.Services.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Commands.UpdateMarketplaceService;

public sealed record UpdateMarketplaceServiceCommand(
    Guid Id,
    string Code,
    LocalizedText Name,
    LocalizedText? Description,
    int DisplayOrder = 0) : IRequest<MarketplaceServiceDto>;
