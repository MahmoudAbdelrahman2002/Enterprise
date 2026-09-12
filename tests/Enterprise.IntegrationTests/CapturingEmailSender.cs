using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Enterprise.Application.Common.Interfaces;

namespace Enterprise.IntegrationTests;

/// <summary>
/// Captures OTP codes from outbound emails so integration tests can complete verify flows
/// without a real SMTP server (SMTP is empty in tests → EmailSender would only log).
/// </summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private static readonly Regex OtpRegex = new(@"<strong>(\d{4,10})</strong>", RegexOptions.Compiled);

    public static ConcurrentDictionary<string, string> CapturedOtps { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var match = OtpRegex.Match(htmlBody);
        if (match.Success)
        {
            CapturedOtps[toEmail.Trim().ToLowerInvariant()] = match.Groups[1].Value;
        }

        return Task.CompletedTask;
    }
}
