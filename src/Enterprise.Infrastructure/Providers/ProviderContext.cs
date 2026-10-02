using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;

namespace Enterprise.Infrastructure.Providers;

public sealed class ProviderContext(
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork) : IProviderContext
{
    public async Task<Guid> GetProviderIdAsync(CancellationToken cancellationToken = default)
    {
        if (currentUserService.ProviderId.HasValue)
        {
            return currentUserService.ProviderId.Value;
        }

        var userId = currentUserService.UserId ?? throw new ForbiddenAccessException();
        var provider = await unitOfWork.Providers.GetByUserIdAsync(userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), userId);

        return provider.Id;
    }
}
