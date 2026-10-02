using Enterprise.Application.Features.Provider.Products.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Products.Commands.DeleteProductImage;

public sealed record DeleteProductImageCommand(Guid CategoryId, Guid Id) : IRequest<ProductDetailDto>;
