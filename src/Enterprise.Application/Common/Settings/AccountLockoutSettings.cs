namespace Enterprise.Application.Common.Settings;

public sealed class AccountLockoutSettings
{
    public const string SectionName = "AccountLockout";

    public int MaxFailedAccessAttempts { get; init; } = 5;
    public int LockoutDurationMinutes { get; init; } = 15;
}
