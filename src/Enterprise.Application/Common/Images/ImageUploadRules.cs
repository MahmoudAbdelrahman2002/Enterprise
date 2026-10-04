using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Interfaces;
using System.Buffers.Binary;

namespace Enterprise.Application.Common.Images;

public static class ImageUploadRules
{
    public const long MaxBytes = Enterprise.Application.Common.Validation.ValidationPolicy.ImageMaxBytes;

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

    public static async Task<string?> ValidateContentAsync(ImageUploadFile file, CancellationToken cancellationToken)
    {
        var error = Validate(file.ContentType, file.Length);
        if (error is not null) return error;
        if (!file.Content.CanRead || !file.Content.CanSeek) return MessageKeys.Validation.ImageContent;
        var position = file.Content.Position;
        try
        {
            file.Content.Position = 0;
            var bytes = new byte[checked((int)file.Length)];
            var read = 0;
            while (read < bytes.Length)
            {
                var count = await file.Content.ReadAsync(bytes.AsMemory(read), cancellationToken);
                if (count == 0) break;
                read += count;
            }
            if (read != file.Length || file.Content.Length != file.Length) return MessageKeys.Validation.ImageContent;
            var valid = file.ContentType.ToLowerInvariant() switch
            {
                "image/png" => bytes.Length >= 45 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                    && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8) && bytes.AsSpan(bytes.Length - 8, 4).SequenceEqual("IEND"u8),
                "image/jpeg" or "image/jpg" => bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255
                    && bytes[^2] == 255 && bytes[^1] == 217,
                "image/webp" => bytes.Length >= 20 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                    && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)
                    && BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4)) == bytes.Length - 8,
                _ => false
            };
            return valid ? null : MessageKeys.Validation.ImageContent;
        }
        finally { file.Content.Position = position; }
    }

    public static string ResolveExtension(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
}
