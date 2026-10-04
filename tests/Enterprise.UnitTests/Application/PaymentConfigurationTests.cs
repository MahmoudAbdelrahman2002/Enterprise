using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Settings;
using Enterprise.Application.Features.Client.Payments.Commands.CreatePaymentCommand;
using Enterprise.Application.Features.Client.Payments;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Moq;

namespace Enterprise.UnitTests.Application;

[TestFixture]
public sealed class PaymentConfigurationTests
{
    [TestCase("")]
    [TestCase(" ")]
    public void MissingSecretKey_FailsBeforeLoadingCartOrCallingStripe(string secretKey)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(user => user.UserId).Returns(Guid.NewGuid());
        var handler = new CreatePaymentCommandHandler(
            new Mock<IUnitOfWork>(MockBehavior.Strict).Object,
            new Mock<IClientProviderQueryService>(MockBehavior.Strict).Object,
            currentUser.Object,
            Mock.Of<ICurrentCulture>(),
            Options.Create(new StripeSettings { SecretKey = secretKey }),
            new Mock<ICheckoutGateway>(MockBehavior.Strict).Object);

        var error = Assert.ThrowsAsync<PaymentUnavailableException>(() =>
            handler.Handle(new CreatePaymentCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.That(error!.ErrorCode, Is.EqualTo("Payment.Unavailable"));
    }
}
