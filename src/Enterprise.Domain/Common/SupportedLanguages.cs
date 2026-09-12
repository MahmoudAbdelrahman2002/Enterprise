using System.Globalization;

namespace Enterprise.Domain.Common;

/// <summary>
/// Canonical language codes for API culture and catalog translations.
/// Unknown values fall back to English.
/// </summary>
public static class SupportedLanguages
{
    public const string English = "en";
    public const string Italian = "it";
    public const string Arabic = "ar";

    public static readonly IReadOnlyList<string> All = [English, Italian, Arabic];

    public static string Normalize(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return English;
        }

        var twoLetter = culture.Split('-', 2)[0].ToLowerInvariant();
        return All.Contains(twoLetter, StringComparer.OrdinalIgnoreCase) ? twoLetter : English;
    }

    public static string Current => Normalize(CultureInfo.CurrentUICulture.Name);

    public static bool IsSupported(string? languageCode) =>
        !string.IsNullOrWhiteSpace(languageCode)
        && All.Contains(Normalize(languageCode), StringComparer.OrdinalIgnoreCase);
}
