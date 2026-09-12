using Enterprise.Domain.Common;
using Enterprise.Domain.Enums;

namespace Enterprise.Domain.Entities;

/// <summary>
/// A long-lived credential for service-to-service authentication. Only <see cref="KeyHash"/>
/// (SHA-256 of the secret) is persisted; <see cref="KeyPrefix"/> is stored in the clear so the
/// owner can recognize which key a row represents without storing a replayable secret.
/// Phase 1: membership moved to ASP.NET Core Identity; this entity keeps a UserId FK only.
/// </summary>
public sealed class ApiKey : BaseAuditableEntity
{
    private ApiKey() { }

    public ApiKey(Guid userId, string name, string keyPrefix, string keyHash, DateTime? expiresAtUtc)
    {
        UserId = userId;
        Name = name;
        KeyPrefix = keyPrefix;
        KeyHash = keyHash;
        ExpiresAtUtc = expiresAtUtc;
        Status = ApiKeyStatus.Active;
    }

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string KeyPrefix { get; private set; } = null!;
    public string KeyHash { get; private set; } = null!;
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? LastUsedAtUtc { get; private set; }
    public ApiKeyStatus Status { get; private set; }

    public bool IsExpired => ExpiresAtUtc.HasValue && DateTime.UtcNow >= ExpiresAtUtc.Value;
    public bool IsUsable => Status == ApiKeyStatus.Active && !IsExpired;

    public void RecordUsage() => LastUsedAtUtc = DateTime.UtcNow;

    public void Revoke() => Status = ApiKeyStatus.Revoked;
}
