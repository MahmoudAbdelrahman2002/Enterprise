using Enterprise.Application.Common.Interfaces;

namespace Enterprise.Application.Common.Authorization;

public static class PermissionLocalizer
{
    public static IReadOnlyList<PermissionGroupDto> Group(
        IEnumerable<PermissionDefinition> permissions,
        IAppLocalizer localizer)
    {
        return permissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto(
                g.Key,
                g.Select(p => new PermissionItemDto(
                    p.Name,
                    p.Action,
                    Text(localizer, $"Permission.{p.Name}", p.Description ?? p.Name))).ToList(),
                Text(localizer, $"Permission.Module.{g.Key}", g.Key)))
            .ToList();
    }

    private static string Text(IAppLocalizer localizer, string key, string fallback)
    {
        var value = localizer[key];
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal)
            ? fallback
            : value;
    }
}
