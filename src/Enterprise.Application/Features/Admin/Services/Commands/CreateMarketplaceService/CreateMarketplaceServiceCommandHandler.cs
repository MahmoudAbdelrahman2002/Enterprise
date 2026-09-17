using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Commands.CreateMarketplaceService;

public sealed class CreateMarketplaceServiceCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture culture) : IRequestHandler<CreateMarketplaceServiceCommand, MarketplaceServiceDto>
{
    public async Task<MarketplaceServiceDto> Handle(
        CreateMarketplaceServiceCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.Services.CodeExistsAsync(request.Code, cancellationToken: cancellationToken))
        {
            throw new ConflictException(MessageKeys.Service.CodeExists);
        }

        var service = new MarketplaceService(request.Code, request.DisplayOrder, request.IsActive);
        service.ApplyLocalizedContent(request.Name, request.Description);

        unitOfWork.Services.Add(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        // return the created service
        return service.ToDto(culture.LanguageCode);
    }
}
