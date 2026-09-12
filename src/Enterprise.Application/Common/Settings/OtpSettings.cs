namespace Enterprise.Application.Common.Settings;

public sealed class OtpSettings
{
    public const string SectionName = "Otp";

    public int Length { get; init; } = 6;
    public int ExpirationMinutes { get; init; } = 10;
    public int MaxAttempts { get; init; } = 5;

    /// <summary>
    /// When true (typical for Development), API responses include the OTP so you can
    /// verify without reading SMTP/Mailtrap. Never enable in production.
    /// </summary>
    public bool ExposeCodeInResponse { get; init; }
}
