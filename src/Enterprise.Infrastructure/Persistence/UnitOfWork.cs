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
    private IProductRepository? _products;
    private IOrderRepository? _orders;
    private ICartRepository? _carts;
    private INotificationRepository? _notifications;
    private IDeviceTokenRepository? _deviceTokens;
    public IProviderRepository Providers => _providers ??= new ProviderRepository(context);
    public IMarketplaceServiceRepository Services => _services ??= new MarketplaceServiceRepository(context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(context);
    public IApiKeyRepository ApiKeys => _apiKeys ??= new ApiKeyRepository(context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(context);
    public ICartRepository Carts => _carts ??= new CartRepository(context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(context);
    public IProductRepository Products => _products ??= new ProductRepository(context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(context);
    public IDeviceTokenRepository DeviceTokens => _deviceTokens ??= new DeviceTokenRepository(context);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
