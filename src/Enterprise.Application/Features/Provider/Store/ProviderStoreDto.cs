using Enterprise.Application.Features.Providers;

namespace Enterprise.Application.Features.Provider.Store;

public sealed record ProviderStoreDto(
    Guid Id,
    string CompanyName,
    string? PhoneNumber,
    string? ImageUrl,
    Guid? ServiceId,
    string? ServiceName)
{
    public static ProviderStoreDto From(ProviderDto provider) =>
        new(provider.Id, provider.CompanyName, provider.PhoneNumber, provider.ImageUrl, provider.ServiceId, provider.ServiceName);
}
