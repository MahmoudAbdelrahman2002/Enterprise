using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Products.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Products.Commands.UploadProductImage;

public sealed record UploadProductImageCommand(Guid CategoryId, Guid Id, ImageUploadFile File)
    : IRequest<ProductDetailDto>;
