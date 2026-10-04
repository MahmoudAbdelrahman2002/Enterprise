using System.Globalization;
using System.Text.RegularExpressions;

namespace Enterprise.Application.Common.Validation;

public static class FieldFormats
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(100);
    public static bool PlainText(string? value, bool multiline = false) => value is null ||
        !value.Any(c => char.IsControl(c) && !(multiline && (c == '\n' || c == '\r' || c == '\t')));

    public static bool PersonName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var hasLetter = false;
        foreach (var rune in value.EnumerateRunes())
        {
            if (System.Text.Rune.IsLetter(rune)) { hasLetter = true; continue; }
            var category = System.Text.Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark) continue;
            if (rune.Value is ' ' or '.' or '\'' or '-' or 0x2019) continue;
            return false;
        }
        return hasLetter;
    }

    public static bool Email(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > ValidationPolicy.EmailMax || value != value.Trim()) return false;
        var parts = value.Split('@');
        if (parts.Length != 2 || parts[0].Length is < 1 or > 64 || parts[0].StartsWith('.') || parts[0].EndsWith('.') || parts[0].Contains("..")) return false;
        if (!Regex.IsMatch(parts[0], @"^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~\-]+$", RegexOptions.None, Timeout)) return false;
        try
        {
            var domain = new IdnMapping().GetAscii(parts[1]);
            var labels = domain.Split('.');
            return domain.Length <= 253 && labels.Length >= 2 && labels.All(label => label.Length is >= 1 and <= 63 &&
                Regex.IsMatch(label, @"^[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?$", RegexOptions.None, Timeout));
        }
        catch (ArgumentException) { return false; }
    }

    public static bool Phone(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        if (string.IsNullOrWhiteSpace(value) || value.Length > ValidationPolicy.PhoneMax ||
            !Regex.IsMatch(value, @"^\+?[0-9 ()\-]+$", RegexOptions.None, Timeout)) return false;
        var digits = value.Count(c => c is >= '0' and <= '9');
        var depth = 0;
        foreach (var c in value) { if (c == '(' && ++depth > 1) return false; if (c == ')' && --depth < 0) return false; }
        return depth == 0 && digits >= ValidationPolicy.PhoneDigitsMin && digits <= ValidationPolicy.PhoneDigitsMax;
    }
}
