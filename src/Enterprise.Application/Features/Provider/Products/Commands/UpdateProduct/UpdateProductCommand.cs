using Enterprise.Application.Features.Provider.Products.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(Guid CategoryId, Guid Id, UpdateProductDto Product)
    : IRequest<ProductDetailDto>;
