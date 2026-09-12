using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

/// <summary>
/// Only the SHA-256 hash of the refresh token is persisted - never the raw value - mirroring
/// password-hashing discipline: a leaked database row must not itself be a usable credential.
/// Implements rotation: each successful refresh revokes the old token and links it to its
/// replacement (<see cref="ReplacedByTokenHash"/>), so token re-use after rotation is detectable
/// and can be treated as a signal of theft (the whole family can be revoked).
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc, string? createdByIp)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? ReasonRevoked { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? revokedByIp, string reason, string? replacedByTokenHash = null)
    {
        RevokedAtUtc = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
        ReasonRevoked = reason;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
