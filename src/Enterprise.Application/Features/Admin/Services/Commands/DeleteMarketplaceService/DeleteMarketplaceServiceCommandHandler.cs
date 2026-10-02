using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Services.Commands.DeleteMarketplaceService;

public sealed class DeleteMarketplaceServiceCommandHandler(
    IUnitOfWork unitOfWork,
    ILogger<DeleteMarketplaceServiceCommandHandler> logger)
    : IRequestHandler<DeleteMarketplaceServiceCommand>
{
    public async Task Handle(DeleteMarketplaceServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await unitOfWork.Services.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(MarketplaceService), request.Id);

        if (await unitOfWork.Services.HasLinkedProvidersAsync(request.Id, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Service.HasLinkedProviders);
        }

        unitOfWork.Services.Remove(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted marketplace service {ServiceId}", request.Id);
    }
}
