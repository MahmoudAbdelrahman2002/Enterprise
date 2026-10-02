using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Admin.Services.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Commands.UploadMarketplaceServiceImage;

public sealed record UploadMarketplaceServiceImageCommand(Guid Id, ImageUploadFile File)
    : IRequest<MarketplaceServiceDto>;
