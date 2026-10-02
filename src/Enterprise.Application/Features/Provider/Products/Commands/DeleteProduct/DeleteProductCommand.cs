using MediatR;

namespace Enterprise.Application.Features.Provider.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid CategoryId, Guid Id) : IRequest;
