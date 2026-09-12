using Enterprise.Domain.Interfaces;
using Enterprise.Infrastructure.Persistence.Repositories;

namespace Enterprise.Infrastructure.Persistence;

public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private IProductRepository? _products;
    private IRefreshTokenRepository? _refreshTokens;
    private IApiKeyRepository? _apiKeys;

    public IProductRepository Products => _products ??= new ProductRepository(context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(context);
    public IApiKeyRepository ApiKeys => _apiKeys ??= new ApiKeyRepository(context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
