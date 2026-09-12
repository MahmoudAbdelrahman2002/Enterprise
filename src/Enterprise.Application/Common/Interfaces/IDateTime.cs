namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// Thin wrapper around <see cref="DateTime.UtcNow"/> so handlers that branch on "now" (token
/// expiry, lockout windows) can be unit tested deterministically by substituting a fake clock
/// instead of depending on wall-clock time.
/// </summary>
public interface IDateTime
{
    DateTime UtcNow { get; }
}
