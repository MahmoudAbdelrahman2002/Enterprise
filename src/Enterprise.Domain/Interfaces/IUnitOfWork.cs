namespace Enterprise.Domain.Interfaces;

/// <summary>
/// Wraps a single EF Core <c>DbContext</c> change-tracking scope. Exposes named,
/// already-typed repositories so callers get compile-time access to entity-specific query
/// methods without casting. All repositories obtained from the same instance share one
/// DbContext, so a single <see cref="SaveChangesAsync"/> commits every change atomically.
/// </summary>
public interface IUnitOfWork
{
    IProviderRepository Providers { get; }
    IMarketplaceServiceRepository Services { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IApiKeyRepository ApiKeys { get; }
    ICategoryRepository Categories { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
