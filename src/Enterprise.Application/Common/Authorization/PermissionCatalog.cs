using Enterprise.Domain.Enums;

namespace Enterprise.Application.Common.Authorization;

public sealed record PermissionItemDto(string Name, string Action, string? Description);

public sealed record PermissionGroupDto(string Module, IReadOnlyList<PermissionItemDto> Permissions);

public sealed record PermissionDefinition(
    string Name,
    UserType Portal,
    string Module,
    string Action,
    string? Description = null);

public static class Permissions
{
    public static class Providers
    {
        public const string Read = "Providers.Read";
        public const string Create = "Providers.Create";
        public const string Update = "Providers.Update";
        public const string Delete = "Providers.Delete";
    }

    public static class Roles
    {
        public const string Read = "Roles.Read";
        public const string Create = "Roles.Create";
        public const string Update = "Roles.Update";
        public const string Delete = "Roles.Delete";
    }

    public static class ApiKeys
    {
        public const string Create = "ApiKeys.Create";
    }

    public static class Admins
    {
        public const string Read = "Admins.Read";
        public const string Create = "Admins.Create";
        public const string Update = "Admins.Update";
        public const string Delete = "Admins.Delete";
    }

    public static class Services
    {
        public const string Read = "Services.Read";
        public const string Create = "Services.Create";
        public const string Update = "Services.Update";
        public const string Delete = "Services.Delete";
    }

    public static class ProviderRoles
    {
        public const string Read = "ProviderRoles.Read";
        public const string Create = "ProviderRoles.Create";
        public const string Update = "ProviderRoles.Update";
        public const string Delete = "ProviderRoles.Delete";
    }

    public static class ProviderStaff
    {
        public const string Read = "ProviderStaff.Read";
        public const string Create = "ProviderStaff.Create";
        public const string Update = "ProviderStaff.Update";
        public const string Delete = "ProviderStaff.Delete";
    }

    public static class ProviderCategory
    {
        public const string Read = "ProviderCategory.Read";
        public const string Create = "ProviderCategory.Create";
        public const string Update = "ProviderCategory.Update";
        public const string Delete = "ProviderCategory.Delete";
    }
}

public static class PermissionCatalog
{
    private static readonly List<PermissionDefinition> _allPermissions =
    [
        // Admin Portal - Providers Module
        new(Permissions.Providers.Read, UserType.Admin, "Providers", "Read", "View marketplace providers"),
        new(Permissions.Providers.Create, UserType.Admin, "Providers", "Create", "Onboard new marketplace providers"),
        new(Permissions.Providers.Update, UserType.Admin, "Providers", "Update", "Update provider details and active state"),
        new(Permissions.Providers.Delete, UserType.Admin, "Providers", "Delete", "Remove marketplace providers"),

        // Admin Portal - Roles Module
        new(Permissions.Roles.Read, UserType.Admin, "Roles", "Read", "View administrative roles and assigned permissions"),
        new(Permissions.Roles.Create, UserType.Admin, "Roles", "Create", "Create administrative roles with permissions"),
        new(Permissions.Roles.Update, UserType.Admin, "Roles", "Update", "Modify administrative roles and permissions"),
        new(Permissions.Roles.Delete, UserType.Admin, "Roles", "Delete", "Delete administrative custom roles"),

        // Admin Portal - ApiKeys Module
        new(Permissions.ApiKeys.Create, UserType.Admin, "ApiKeys", "Create", "Generate service API keys"),

        // Admin Portal - Admins Module
        new(Permissions.Admins.Read, UserType.Admin, "Admins", "Read", "View platform administrative staff"),
        new(Permissions.Admins.Create, UserType.Admin, "Admins", "Create", "Create new administrative staff"),
        new(Permissions.Admins.Update, UserType.Admin, "Admins", "Update", "Update administrative staff and status"),
        new(Permissions.Admins.Delete, UserType.Admin, "Admins", "Delete", "Delete administrative staff"),

        // Admin Portal - Services Module
        new(Permissions.Services.Read, UserType.Admin, "Services", "Read", "View marketplace services"),
        new(Permissions.Services.Create, UserType.Admin, "Services", "Create", "Create new marketplace services"),
        new(Permissions.Services.Update, UserType.Admin, "Services", "Update", "Update marketplace services and translations"),
        new(Permissions.Services.Delete, UserType.Admin, "Services", "Delete", "Delete marketplace services"),

        // Provider Portal - Provider Roles Module
        new(Permissions.ProviderRoles.Read, UserType.Provider, "ProviderRoles", "Read", "View store staff roles"),
        new(Permissions.ProviderRoles.Create, UserType.Provider, "ProviderRoles", "Create", "Create store staff roles"),
        new(Permissions.ProviderRoles.Update, UserType.Provider, "ProviderRoles", "Update", "Modify store staff roles"),
        new(Permissions.ProviderRoles.Delete, UserType.Provider, "ProviderRoles", "Delete", "Delete store staff roles"),

        // Provider Portal - ProviderStaff Module
        new(Permissions.ProviderStaff.Read, UserType.Provider, "ProviderStaff", "Read", "View store staff members"),
        new(Permissions.ProviderStaff.Create, UserType.Provider, "ProviderStaff", "Create", "Create store staff members"),
        new(Permissions.ProviderStaff.Update, UserType.Provider, "ProviderStaff", "Update", "Update store staff and status"),
        new(Permissions.ProviderStaff.Delete, UserType.Provider, "ProviderStaff", "Delete", "Delete store staff members"),

        // Provider Portal - ProviderCategory Module
        new(Permissions.ProviderCategory.Read, UserType.Provider, "ProviderCategory", "Read", "View store categories"),
        new(Permissions.ProviderCategory.Create, UserType.Provider, "ProviderCategory", "Create", "Create store categories"),
        new(Permissions.ProviderCategory.Update, UserType.Provider, "ProviderCategory", "Update", "Update store categories"),
        new(Permissions.ProviderCategory.Delete, UserType.Provider, "ProviderCategory", "Delete", "Delete store categories"),
    ];

    public static IReadOnlyList<PermissionDefinition> All => _allPermissions;

    public static IReadOnlyList<PermissionDefinition> AdminPermissions =>
        _allPermissions.Where(p => p.Portal == UserType.Admin).ToList();

    public static IReadOnlyList<PermissionDefinition> ProviderPermissions =>
        _allPermissions.Where(p => p.Portal == UserType.Provider).ToList();

    public static bool IsValidForPortal(UserType portal, string permissionName) =>
        _allPermissions.Any(p => p.Portal == portal && string.Equals(p.Name, permissionName, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> GetNamesForPortal(UserType portal) =>
        _allPermissions
            .Where(p => p.Portal == portal)
            .Select(p => p.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
