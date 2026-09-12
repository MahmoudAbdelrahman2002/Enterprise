namespace Enterprise.Application.Common.Settings;

public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string From { get; init; } = "noreply@enterprise.local";

    /// <summary>Friendly From name shown in the inbox (e.g. "Enterprise").</summary>
    public string FromDisplayName { get; init; } = "Enterprise";

    /// <summary>
    /// Laravel-style: <c>tls</c> (STARTTLS, port 587), <c>ssl</c> (implicit SSL, port 465), or <c>none</c>.
    /// </summary>
    public string Encryption { get; init; } = "tls";

    /// <summary>
    /// When false, skips CRL/OCSP revocation checks (needed on some Windows networks).
    /// Keep true in production when revocation endpoints are reachable.
    /// </summary>
    public bool CheckCertificateRevocation { get; init; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(User)
        && !string.IsNullOrWhiteSpace(Password);
}
