using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace Enterprise.UnitTests.Domain;

[TestFixture]
public class ApiKeyTests
{
    [Test]
    public void NewApiKey_WithNoExpiry_IsUsable()
    {
        var apiKey = new ApiKey(Guid.NewGuid(), "CI key", "ek_live_abcd1234", "hash", expiresAtUtc: null);

        apiKey.IsExpired.Should().BeFalse();
        apiKey.IsUsable.Should().BeTrue();
    }

    [Test]
    public void ApiKey_PastExpiry_IsNotUsable()
    {
        var apiKey = new ApiKey(Guid.NewGuid(), "CI key", "ek_live_abcd1234", "hash", DateTime.UtcNow.AddMinutes(-1));

        apiKey.IsExpired.Should().BeTrue();
        apiKey.IsUsable.Should().BeFalse();
    }

    [Test]
    public void Revoke_SetsStatusRevokedAndNotUsable()
    {
        var apiKey = new ApiKey(Guid.NewGuid(), "CI key", "ek_live_abcd1234", "hash", expiresAtUtc: null);

        apiKey.Revoke();

        apiKey.Status.Should().Be(ApiKeyStatus.Revoked);
        apiKey.IsUsable.Should().BeFalse();
    }

    [Test]
    public void RecordUsage_SetsLastUsedAtUtc()
    {
        var apiKey = new ApiKey(Guid.NewGuid(), "CI key", "ek_live_abcd1234", "hash", expiresAtUtc: null);

        apiKey.RecordUsage();

        apiKey.LastUsedAtUtc.Should().NotBeNull();
    }
}
