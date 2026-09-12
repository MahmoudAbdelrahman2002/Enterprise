namespace Enterprise.Application.Common.Auth;

public static class ExternalAuthProviders
{
    public const string Google = "Google";
    public const string Facebook = "Facebook";

    public static bool IsSupported(string provider) =>
        provider.Equals(Google, StringComparison.OrdinalIgnoreCase)
        || provider.Equals(Facebook, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string provider) =>
        provider.Equals(Facebook, StringComparison.OrdinalIgnoreCase) ? Facebook : Google;
}
