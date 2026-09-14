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
    public static class Products
    {
        public const string Read = "Products.Read";
        public const string Create = "Products.Create";
        public const string Update = "Products.Update";
        public const string Delete = "Products.Delete";
    }

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

    public static class ProviderRoles
    {
        public const string Read = "ProviderRoles.Read";
        public const string Create = "ProviderRoles.Create";
        public const string Update = "ProviderRoles.Update";
        public const string Delete = "ProviderRoles.Delete";
    }
}

public static class PermissionCatalog
{
    private static readonly List<PermissionDefinition> _allPermissions =
    [
        // Admin Portal - Products Module
        new(Permissions.Products.Read, UserType.Admin, "Products", "Read", "View products in the catalog"),
        new(Permissions.Products.Create, UserType.Admin, "Products", "Create", "Create new catalog products"),
        new(Permissions.Products.Update, UserType.Admin, "Products", "Update", "Update existing catalog products"),
        new(Permissions.Products.Delete, UserType.Admin, "Products", "Delete", "Delete catalog products"),

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

        // Provider Portal - Provider Roles Module
        new(Permissions.ProviderRoles.Read, UserType.Provider, "ProviderRoles", "Read", "View store staff roles"),
        new(Permissions.ProviderRoles.Create, UserType.Provider, "ProviderRoles", "Create", "Create store staff roles"),
        new(Permissions.ProviderRoles.Update, UserType.Provider, "ProviderRoles", "Update", "Modify store staff roles"),
        new(Permissions.ProviderRoles.Delete, UserType.Provider, "ProviderRoles", "Delete", "Delete store staff roles"),

        // Provider Portal - Products Module (Catalog browsing)
        new(Permissions.Products.Read, UserType.Provider, "Products", "Read", "Browse general product catalog")
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
