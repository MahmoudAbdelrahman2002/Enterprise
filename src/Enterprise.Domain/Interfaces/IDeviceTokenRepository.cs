using Enterprise.Domain.Entities;

namespace Enterprise.Domain.Interfaces;

public interface IDeviceTokenRepository : IRepository<DeviceToken>
{
    Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceToken>> ListByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task RemoveByTokenAsync(string token, CancellationToken cancellationToken = default);
}
