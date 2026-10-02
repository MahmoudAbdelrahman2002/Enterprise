using Enterprise.Domain.Entities;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Identity;

internal static class RefreshTokenSession
{
    public static async Task RevokeAllActiveAsync(
        ApplicationDbContext context,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null && rt.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return;
        }

        foreach (var token in tokens)
        {
            token.Revoke(revokedByIp: null, reason);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
