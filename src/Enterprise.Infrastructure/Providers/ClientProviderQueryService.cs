using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Providers;

public sealed class ClientProviderQueryService(
    ApplicationDbContext context,
    ICurrentCulture culture) : IClientProviderQueryService
{
    public async Task<IReadOnlyList<Guid>> GetActiveServiceIdsAsync(CancellationToken cancellationToken = default) =>
        await ActiveProviders()
            .Where(provider => provider.ServiceId.HasValue)
            .Select(provider => provider.ServiceId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<ClientProviderListItemDto>> GetPagedByServiceIdAsync(
        Guid serviceId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query =
            from provider in ActiveProviders()
            where provider.ServiceId == serviceId
            select provider;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(provider => provider.CompanyName.Contains(term));
        }

        var page = pageNumber < 1 ? 1 : pageNumber;
        var totalCount = await query.CountAsync(cancellationToken);

        var rawRows = await query
            .OrderBy(provider => provider.CompanyName)
            .ThenBy(provider => provider.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rawRows.Select(ToDto).ToList();
        return new PagedResult<ClientProviderListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<ClientProviderListItemDto?> GetByIdAsync(
        Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var provider = await ActiveProviders()
            .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);

        return provider is null ? null : ToDto(provider);
    }

    private IQueryable<Domain.Entities.Provider> ActiveProviders() =>
        context.Providers.AsNoTracking()
            .Include(provider => provider.Service)
            .Where(provider => context.Users.Any(user =>
                user.Id == provider.UserId
                && user.UserType == UserType.Provider
                && user.IsActive));

    private ClientProviderListItemDto ToDto(Domain.Entities.Provider provider)
    {
        var serviceName = provider.Service?.ResolveContent(culture.LanguageCode).Name;
        return new ClientProviderListItemDto(
            provider.Id,
            provider.CompanyName,
            provider.PhoneNumber,
            provider.ImageUrl,
            provider.ServiceId,
            serviceName);
    }
}
