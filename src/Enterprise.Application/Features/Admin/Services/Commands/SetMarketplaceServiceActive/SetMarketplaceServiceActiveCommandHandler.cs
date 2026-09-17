using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Commands.SetMarketplaceServiceActive;

public sealed class SetMarketplaceServiceActiveCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture culture) : IRequestHandler<SetMarketplaceServiceActiveCommand, MarketplaceServiceDto>
{
    public async Task<MarketplaceServiceDto> Handle(
        SetMarketplaceServiceActiveCommand request, CancellationToken cancellationToken)
    {
        var service = await unitOfWork.Services.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(MarketplaceService), request.Id);

        service.SetActive(request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return service.ToDto(culture.LanguageCode);
    }
}
