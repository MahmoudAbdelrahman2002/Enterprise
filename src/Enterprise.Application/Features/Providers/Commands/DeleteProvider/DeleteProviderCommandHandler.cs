using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Providers.Commands.DeleteProvider;

public sealed class DeleteProviderCommandHandler(
    IUnitOfWork unitOfWork,
    IUserAccountService userAccountService) : IRequestHandler<DeleteProviderCommand>
{
    public async Task Handle(DeleteProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await unitOfWork.Providers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.Entities.Provider), request.Id);

        var user = await userAccountService.FindByIdAsync(provider.UserId, cancellationToken);
        if (user is not null && user.IsSystem)
        {
            throw new ConflictException(Enterprise.Application.Common.Localization.MessageKeys.Role.CannotDeleteSystemUser);
        }

        await userAccountService.SetActiveAsync(provider.UserId, isActive: false, cancellationToken);

        unitOfWork.Providers.Remove(provider);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
