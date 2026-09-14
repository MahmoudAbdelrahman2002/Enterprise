using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Providers.Commands.SetProviderActive;

public sealed class SetProviderActiveCommandHandler(
    IUnitOfWork unitOfWork,
    IUserAccountService userAccountService,
    IProviderAdminQueryService providerAdminQueryService) : IRequestHandler<SetProviderActiveCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(SetProviderActiveCommand request, CancellationToken cancellationToken)
    {
        var provider = await unitOfWork.Providers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.Entities.Provider), request.Id);

        await userAccountService.SetActiveAsync(provider.UserId, request.IsActive, cancellationToken);

        var detail = await providerAdminQueryService.GetByIdAsync(provider.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.Entities.Provider), request.Id);

        return detail.ToDto();
    }
}
