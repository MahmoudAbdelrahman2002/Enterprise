using System.Net;
using System.Text.RegularExpressions;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Enterprise.Infrastructure.Email;

public sealed class EmailSender(
    IOptions<SmtpSettings> smtpOptions,
    ILogger<EmailSender> logger) : IEmailSender
{
    private readonly SmtpSettings _smtp = smtpOptions.Value;

    public async Task SendAsync(
        string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!_smtp.IsConfigured)
        {
            logger.LogWarning(
                "SMTP not configured. Email to {To} subject {Subject}. Body: {Body}",
                toEmail, subject, htmlBody);
            return;
        }

        try
        {
            var fromAddress = string.IsNullOrWhiteSpace(_smtp.From) ? _smtp.User : _smtp.From;
            var displayName = string.IsNullOrWhiteSpace(_smtp.FromDisplayName)
                ? "Enterprise"
                : _smtp.FromDisplayName;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(displayName, fromAddress.Trim()));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Headers.Add("X-Entity-Ref-ID", Guid.NewGuid().ToString("N"));

            var plainText = HtmlToPlainText(htmlBody);
            var builder = new BodyBuilder
            {
                TextBody = plainText,
                HtmlBody = htmlBody
            };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            client.CheckCertificateRevocation = _smtp.CheckCertificateRevocation;

            try
            {
                await client.ConnectAsync(_smtp.Host, _smtp.Port, ResolveEncryption(), cancellationToken);

                if (!string.IsNullOrWhiteSpace(_smtp.User))
                {
                    var password = _smtp.Password.Replace(" ", string.Empty, StringComparison.Ordinal);
                    await client.AuthenticateAsync(_smtp.User.Trim(), password, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);

                if (_smtp.Host.Contains("mailtrap", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "SMTP host is Mailtrap sandbox — mail is NOT delivered to a real inbox. Open https://mailtrap.io/inboxes or switch Smtp to smtp.gmail.com.");
                }
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true, CancellationToken.None);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To} subject {Subject} via {Host}:{Port}",
                toEmail, subject, _smtp.Host, _smtp.Port);
            throw new EmailDeliveryException(innerException: ex);
        }
    }

    private static string HtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var noTags = Regex.Replace(html, "<[^>]+>", " ");
        return WebUtility.HtmlDecode(noTags).Trim();
    }

    private SecureSocketOptions ResolveEncryption() =>
        _smtp.Encryption.Trim().ToLowerInvariant() switch
        {
            "ssl" => SecureSocketOptions.SslOnConnect,
            "none" or "false" => SecureSocketOptions.None,
            _ => SecureSocketOptions.StartTls
        };
}
