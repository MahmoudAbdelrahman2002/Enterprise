using Enterprise.Application.Common.Models;
using Enterprise.Domain.Common;

namespace Enterprise.Application.Common.Localization;

/// <summary>
/// Shared EN + optional IT/AR upsert with English fallback for missing optional fields.
/// </summary>
public static class LocalizedContentHelper
{
    public static void Apply(
        Action<string, string, string?> upsertTranslation,
        LocalizedText name,
        LocalizedText? description)
    {
        upsertTranslation(
            SupportedLanguages.English,
            name.En,
            description?.En);

        ApplyOptional(upsertTranslation, SupportedLanguages.Italian, name.It, description?.It, name.En, description?.En);
        ApplyOptional(upsertTranslation, SupportedLanguages.Arabic, name.Ar, description?.Ar, name.En, description?.En);
    }

    private static void ApplyOptional(
        Action<string, string, string?> upsertTranslation,
        string language,
        string? name,
        string? description,
        string englishName,
        string? englishDescription)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        upsertTranslation(
            language,
            string.IsNullOrWhiteSpace(name) ? englishName : name,
            string.IsNullOrWhiteSpace(description) ? englishDescription : description);
    }
}
