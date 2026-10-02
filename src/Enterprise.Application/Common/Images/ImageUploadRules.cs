using Enterprise.Application.Common.Localization;

namespace Enterprise.Application.Common.Images;

public static class ImageUploadRules
{
    public const long MaxBytes = 2 * 1024 * 1024;

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    public static string? Validate(string? contentType, long length)
    {
        if (length <= 0)
        {
            return MessageKeys.Image.Required;
        }

        if (length > MaxBytes)
        {
            return MessageKeys.Image.TooLarge;
        }

        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            return MessageKeys.Image.InvalidType;
        }

        return null;
    }

    public static string ResolveExtension(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
}
