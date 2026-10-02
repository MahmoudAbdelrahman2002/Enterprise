using Enterprise.Application.Common.Models;
using Enterprise.Domain.Common;

namespace Enterprise.Infrastructure.Identity;

public sealed class RoleTranslation
{
    public Guid RoleId { get; set; }
    public string LanguageCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public static class RoleDisplayNames
{
    public static string Resolve(IEnumerable<RoleTranslation> translations, string? language, string fallback)
    {
        var code = SupportedLanguages.Normalize(language);
        var requested = translations.FirstOrDefault(t => t.LanguageCode == code)?.Name;
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return requested;
        }

        var english = translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.English)?.Name;
        return !string.IsNullOrWhiteSpace(english) ? english : fallback;
    }

    public static LocalizedText ToLocalizedText(IEnumerable<RoleTranslation> translations, string fallback)
    {
        string? Pick(string code)
        {
            var name = translations.FirstOrDefault(t => t.LanguageCode == code)?.Name;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        return new LocalizedText(
            Pick(SupportedLanguages.English) ?? fallback,
            Pick(SupportedLanguages.Italian),
            Pick(SupportedLanguages.Arabic));
    }
}
