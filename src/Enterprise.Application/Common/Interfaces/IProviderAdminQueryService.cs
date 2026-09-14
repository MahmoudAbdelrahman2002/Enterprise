using Enterprise.Application.Common.Models;

namespace Enterprise.Application.Common.Interfaces;

public sealed record ProviderAdminListItemDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string CompanyName,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record ProviderAdminDetailDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string CompanyName,
    string? PhoneNumber,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAtUtc,
    DateTime? LastModifiedAtUtc);

/// <summary>
/// Read model joining Providers with AspNetUsers for admin management screens.
/// </summary>
public interface IProviderAdminQueryService
{
    Task<PagedResult<ProviderAdminListItemDto>> GetPagedAsync(
        string? searchTerm,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ProviderAdminDetailDto?> GetByIdAsync(Guid providerId, CancellationToken cancellationToken = default);
}
