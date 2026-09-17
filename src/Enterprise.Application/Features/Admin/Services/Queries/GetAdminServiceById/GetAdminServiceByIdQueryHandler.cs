using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Queries.GetAdminServiceById;

public sealed class GetAdminServiceByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentCulture culture)
    : IRequestHandler<GetAdminServiceByIdQuery, MarketplaceServiceDto>
{
    public async Task<MarketplaceServiceDto> Handle(
        GetAdminServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var service = await unitOfWork.Services.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(MarketplaceService), request.Id);

        return service.ToDto(culture.LanguageCode);
    }
}
