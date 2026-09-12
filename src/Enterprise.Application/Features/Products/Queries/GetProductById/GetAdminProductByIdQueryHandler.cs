using Enterprise.Application.Common.Exceptions;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductById;

public sealed class GetAdminProductByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminProductByIdQuery, AdminProductDto>
{
    public async Task<AdminProductDto> Handle(GetAdminProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.Entities.Product), request.Id);

        return product.ToAdminDto();
    }
}
