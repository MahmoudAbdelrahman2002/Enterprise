using Enterprise.Domain.Common;
using Enterprise.Domain.Enums;

namespace Enterprise.Infrastructure.Identity;

public sealed class OtpChallenge : BaseEntity
{
    private OtpChallenge()
    {
    }

    public OtpChallenge(string email, OtpPurpose purpose, string codeHash, DateTime expiresAtUtc)
    {
        Email = email.Trim().ToLowerInvariant();
        Purpose = purpose;
        CodeHash = codeHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Email { get; private set; } = null!;
    public OtpPurpose Purpose { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }

    public bool IsConsumed => ConsumedAtUtc.HasValue;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public void IncrementAttempts() => Attempts++;

    public void Consume() => ConsumedAtUtc = DateTime.UtcNow;
}
