namespace Enterprise.Application.Features.Client.Payments.Commands.CreatePaymentCommand;

public sealed class CreatePaymentSessionDto
{
    public string SessionId { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;

    /// <summary>Echo of Checkout Session metadata returned by Stripe after create.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}
