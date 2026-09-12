using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Enterprise.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// A background job legitimately sits in Infrastructure and talks to
/// <see cref="ApplicationDbContext"/> directly rather than through
/// <c>IUnitOfWork</c>/<c>IRepository</c> - it's a purely operational, bulk cleanup concern with
/// no business meaning, not a use case. <c>ExecuteDeleteAsync</c> issues a single
/// <c>DELETE ... WHERE</c> statement server-side instead of loading every expired row into
/// memory just to delete it, which matters once this table has millions of historical rows.
/// </summary>
public sealed class PurgeExpiredRefreshTokensJob(ApplicationDbContext dbContext, ILogger<PurgeExpiredRefreshTokensJob> logger)
{
    private static readonly TimeSpan RetentionPeriodAfterExpiry = TimeSpan.FromDays(30);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - RetentionPeriodAfterExpiry;

        var deletedCount = await dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Purged {Count} expired refresh tokens older than {Cutoff}.", deletedCount, cutoff);
    }
}
