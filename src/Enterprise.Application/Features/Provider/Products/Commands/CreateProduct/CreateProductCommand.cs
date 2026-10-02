using Enterprise.Application.Features.Provider.Products.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(Guid CategoryId, CreateProductDto Product)
    : IRequest<ProductDetailDto>;
