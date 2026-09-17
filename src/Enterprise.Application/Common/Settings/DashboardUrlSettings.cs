namespace Enterprise.Application.Common.Settings;

public sealed class DashboardUrlSettings
{
    public const string SectionName = "DashboardUrls";

    public string Admin { get; init; } = "http://localhost:4200/admin/login";
    public string Provider { get; init; } = "http://localhost:4200/provider/login";
}
