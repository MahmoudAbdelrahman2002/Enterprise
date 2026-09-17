using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Providers;

public sealed class ProviderAdminQueryService(
    ApplicationDbContext context,
    ICurrentCulture culture) : IProviderAdminQueryService
{
    public async Task<PagedResult<ProviderAdminListItemDto>> GetPagedAsync(
        string? searchTerm,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query =
            from provider in context.Providers.AsNoTracking().Include(p => p.Service)
            join user in context.Users.AsNoTracking() on provider.UserId equals user.Id
            where user.UserType == UserType.Provider
            select new { provider, user };

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(x =>
                x.provider.CompanyName.Contains(term)
                || (x.user.Email != null && x.user.Email.Contains(term))
                || x.user.FirstName.Contains(term)
                || x.user.LastName.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.user.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rawRows = await query
            .OrderByDescending(x => x.provider.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var language = culture.LanguageCode;
        var items = rawRows.Select(x =>
        {
            var serviceName = x.provider.Service?.ResolveContent(language).Name;
            return new ProviderAdminListItemDto(
                x.provider.Id,
                x.provider.UserId,
                x.user.Email ?? string.Empty,
                x.user.FirstName,
                x.user.LastName,
                x.provider.CompanyName,
                x.provider.PhoneNumber,
                x.provider.ServiceId,
                serviceName,
                x.user.IsActive,
                x.provider.CreatedAtUtc);
        }).ToList();

        return new PagedResult<ProviderAdminListItemDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<ProviderAdminDetailDto?> GetByIdAsync(
        Guid providerId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from provider in context.Providers.AsNoTracking().Include(p => p.Service)
            join user in context.Users.AsNoTracking() on provider.UserId equals user.Id
            where provider.Id == providerId && user.UserType == UserType.Provider
            select new { provider, user }
        ).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var language = culture.LanguageCode;
        var serviceName = row.provider.Service?.ResolveContent(language).Name;

        return new ProviderAdminDetailDto(
            row.provider.Id,
            row.provider.UserId,
            row.user.Email ?? string.Empty,
            row.user.FirstName,
            row.user.LastName,
            row.provider.CompanyName,
            row.provider.PhoneNumber,
            row.provider.ServiceId,
            serviceName,
            row.user.IsActive,
            row.user.EmailConfirmed,
            row.provider.CreatedAtUtc,
            row.provider.LastModifiedAtUtc);
    }
}
