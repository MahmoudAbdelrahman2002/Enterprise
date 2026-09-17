using Enterprise.Application.Common.Interfaces;

namespace Enterprise.Application.Features.Providers;

public sealed record ProviderDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string CompanyName,
    string? PhoneNumber,
    Guid? ServiceId,
    string? ServiceName,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAtUtc,
    DateTime? LastModifiedAtUtc);

public static class ProviderDtoMapping
{
    public static ProviderDto ToDto(this ProviderAdminDetailDto detail) =>
        new(
            detail.Id,
            detail.UserId,
            detail.Email,
            detail.FirstName,
            detail.LastName,
            detail.CompanyName,
            detail.PhoneNumber,
            detail.ServiceId,
            detail.ServiceName,
            detail.IsActive,
            detail.EmailConfirmed,
            detail.CreatedAtUtc,
            detail.LastModifiedAtUtc);

    public static ProviderDto ToDto(this ProviderAdminListItemDto item) =>
        new(
            item.Id,
            item.UserId,
            item.Email,
            item.FirstName,
            item.LastName,
            item.CompanyName,
            item.PhoneNumber,
            item.ServiceId,
            item.ServiceName,
            item.IsActive,
            EmailConfirmed: true,
            item.CreatedAtUtc,
            LastModifiedAtUtc: null);
}
