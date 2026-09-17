using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Commands.UpdateMarketplaceService;

public sealed class UpdateMarketplaceServiceCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture culture) : IRequestHandler<UpdateMarketplaceServiceCommand, MarketplaceServiceDto>
{
    public async Task<MarketplaceServiceDto> Handle(
        UpdateMarketplaceServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await unitOfWork.Services.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(MarketplaceService), request.Id);

        if (await unitOfWork.Services.CodeExistsAsync(request.Code, request.Id, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Service.CodeExists);
        }

        service.UpdateDetails(request.Code, request.DisplayOrder);
        service.ApplyLocalizedContent(request.Name, request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return service.ToDto(culture.LanguageCode);
    }
}
