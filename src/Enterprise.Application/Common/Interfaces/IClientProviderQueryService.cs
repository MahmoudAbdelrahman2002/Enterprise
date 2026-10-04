using Enterprise.Application.Common.Models;

namespace Enterprise.Application.Common.Interfaces;

public sealed record ClientProviderListItemDto(
    Guid Id,
    string CompanyName,
    string? PhoneNumber,
    string? ImageUrl,
    Guid? ServiceId,
    string? ServiceName);

/// <summary>
/// Client read model joining Providers with AspNetUsers and localized marketplace service names.
/// </summary>
public interface IClientProviderQueryService
{
    Task<IReadOnlyList<Guid>> GetActiveServiceIdsAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<ClientProviderListItemDto>> GetPagedByServiceIdAsync(
        Guid serviceId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ClientProviderListItemDto?> GetByIdAsync(
        Guid providerId,
        CancellationToken cancellationToken = default);
}
