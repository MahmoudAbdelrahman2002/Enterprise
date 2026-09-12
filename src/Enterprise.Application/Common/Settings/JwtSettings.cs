namespace Enterprise.Application.Common.Settings;

/// <summary>
/// Bound once (via the Options pattern) in the Api composition root from configuration/secrets,
/// then consumed by both <c>JwtTokenService</c> (Infrastructure, needs the signing key) and
/// <c>TokenIssuanceService</c> (Application, needs only the lifetimes) - a single source of
/// truth for token settings instead of duplicated magic numbers in two layers.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; } = 15;
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
