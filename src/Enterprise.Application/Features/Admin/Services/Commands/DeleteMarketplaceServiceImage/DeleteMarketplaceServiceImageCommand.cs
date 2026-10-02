using Enterprise.Application.Features.Admin.Services.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Commands.DeleteMarketplaceServiceImage;

public sealed record DeleteMarketplaceServiceImageCommand(Guid Id) : IRequest<MarketplaceServiceDto>;
