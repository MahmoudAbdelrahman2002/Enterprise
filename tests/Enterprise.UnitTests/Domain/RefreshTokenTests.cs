using Enterprise.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace Enterprise.UnitTests.Domain;

[TestFixture]
public class RefreshTokenTests
{
    [Test]
    public void NewToken_IsActive()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7), "127.0.0.1");

        token.IsActive.Should().BeTrue();
        token.IsExpired.Should().BeFalse();
        token.IsRevoked.Should().BeFalse();
    }

    [Test]
    public void ExpiredToken_IsNotActive()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(-1), "127.0.0.1");

        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Test]
    public void Revoke_MarksTokenAsRevokedAndInactive()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7), "127.0.0.1");

        token.Revoke("127.0.0.2", "logout");

        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
        token.RevokedByIp.Should().Be("127.0.0.2");
        token.ReasonRevoked.Should().Be("logout");
    }

    [Test]
    public void Revoke_WithReplacement_SetsReplacedByTokenHash()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7), "127.0.0.1");

        token.Revoke("127.0.0.2", "Rotated on refresh.", "new-hash");

        token.ReplacedByTokenHash.Should().Be("new-hash");
    }
}
