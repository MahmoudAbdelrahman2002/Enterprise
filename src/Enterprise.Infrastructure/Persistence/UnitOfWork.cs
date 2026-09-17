using Enterprise.Domain.Interfaces;
using Enterprise.Infrastructure.Persistence.Repositories;

namespace Enterprise.Infrastructure.Persistence;

public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private IProviderRepository? _providers;
    private IMarketplaceServiceRepository? _services;
    private IRefreshTokenRepository? _refreshTokens;
    private IApiKeyRepository? _apiKeys;
    private ICategoryRepository? _categories;

    public IProviderRepository Providers => _providers ??= new ProviderRepository(context);
    public IMarketplaceServiceRepository Services => _services ??= new MarketplaceServiceRepository(context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(context);
    public IApiKeyRepository ApiKeys => _apiKeys ??= new ApiKeyRepository(context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(context);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
