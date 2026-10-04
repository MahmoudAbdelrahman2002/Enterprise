using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Auth.Common;
using Enterprise.Application.Features.Provider.Commands.ProviderLogin;
using Enterprise.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Enterprise.UnitTests.Application;

[TestFixture]
public class ProviderLoginTests
{
    [TestCase(true, MessageKeys.Auth.ProviderDeactivated)]
    [TestCase(false, MessageKeys.Auth.InvalidCredentials)]
    public void InactiveProvider_ReceivesDeactivatedMessageOnlyWithCorrectPassword(bool validPassword, string expectedError)
    {
        var user = new AuthUserSnapshot(Guid.NewGuid(), "provider@example.com", "Test", "Provider", UserType.Provider, true, false, [], []);
        var accounts = new Mock<IUserAccountService>();
        var tokens = new Mock<ITokenIssuanceService>();
        accounts.Setup(x => x.FindByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        accounts.Setup(x => x.CheckPasswordAsync(user.Id, "password", It.IsAny<CancellationToken>())).ReturnsAsync(validPassword);
        var handler = new ProviderLoginCommandHandler(accounts.Object, tokens.Object, NullLogger<ProviderLoginCommandHandler>.Instance);

        var error = Assert.ThrowsAsync<AuthenticationFailedException>(() => handler.Handle(new ProviderLoginCommand(user.Email, "password", null), CancellationToken.None));

        Assert.That(error!.ErrorCode, Is.EqualTo(expectedError));
        tokens.Verify(x => x.IssueTokensAsync(It.IsAny<AuthUserSnapshot>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
