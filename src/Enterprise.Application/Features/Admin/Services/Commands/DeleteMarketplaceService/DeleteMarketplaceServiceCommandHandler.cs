using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Commands.DeleteMarketplaceService;

public sealed class DeleteMarketplaceServiceCommandHandler(IUnitOfWork unitOfWork)
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
    }
}
