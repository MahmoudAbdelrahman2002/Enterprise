using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Admin.Services.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Commands.CreateMarketplaceService;

public sealed record CreateMarketplaceServiceCommand(
    string Code,
    LocalizedText Name,
    LocalizedText? Description,
    int DisplayOrder = 0,
    bool IsActive = true) : IRequest<MarketplaceServiceDto>;
