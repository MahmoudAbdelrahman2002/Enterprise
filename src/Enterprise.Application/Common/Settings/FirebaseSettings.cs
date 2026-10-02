namespace Enterprise.Application.Common.Settings;

public sealed class FirebaseSettings
{
    public const string SectionName = "Firebase";

    /// <summary>
    /// Absolute or relative path to a Firebase service-account JSON file.
    /// </summary>
    public string CredentialsPath { get; init; } = string.Empty;

    /// <summary>
    /// Inline service-account JSON (alternative to <see cref="CredentialsPath"/>).
    /// Prefer user-secrets / env vars in non-local environments.
    /// </summary>
    public string CredentialsJson { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CredentialsPath)
        || !string.IsNullOrWhiteSpace(CredentialsJson);
}
