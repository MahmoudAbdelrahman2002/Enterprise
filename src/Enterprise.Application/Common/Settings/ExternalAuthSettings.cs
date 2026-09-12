namespace Enterprise.Application.Common.Settings;

public sealed class ExternalAuthSettings
{
    public const string SectionName = "ExternalAuth";

    public GoogleExternalAuthSettings Google { get; init; } = new();
    public FacebookExternalAuthSettings Facebook { get; init; } = new();
}

public sealed class GoogleExternalAuthSettings
{
    /// <summary>
    /// Allowed OAuth client IDs (aud claim). Include web and mobile client IDs as needed.
    /// </summary>
    public string[] ClientIds { get; init; } = [];
}

public sealed class FacebookExternalAuthSettings
{
    public string AppId { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;
}
