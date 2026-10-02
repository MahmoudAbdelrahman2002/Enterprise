import { PermissionGroupDto, RoleListItemDto } from '../models/domain.models';

export type RolePortal = 'admin' | 'provider';

/** Store-only modules from PermissionCatalog (Provider portal). */
const PROVIDER_MODULES = new Set([
  'ProviderRoles',
  'ProviderStaff',
  'ProviderCategory',
  'ProviderProduct',
  'ProviderOrder',
]);

const PROVIDER_PERMISSION_RE =
  /^(ProviderRoles|ProviderStaff|ProviderCategory|ProviderProduct|ProviderOrder)\./;

/**
 * True for store staff permissions only.
 * Note: admin "Providers.*" must NOT match — that is the marketplace providers module.
 */
export function isProviderPermission(name: string): boolean {
  return PROVIDER_PERMISSION_RE.test(name);
}

export function isAdminPermission(name: string): boolean {
  return !isProviderPermission(name);
}

export function isProviderModule(module: string): boolean {
  return PROVIDER_MODULES.has(module);
}

export function filterPermissionGroupsForPortal(
  groups: PermissionGroupDto[] | null | undefined,
  portal: RolePortal
): PermissionGroupDto[] {
  return (groups ?? [])
    .map((g) => {
      const moduleOk =
        portal === 'provider' ? isProviderModule(g.module) : !isProviderModule(g.module);
      if (!moduleOk) {
        return { module: g.module, permissions: [] as PermissionGroupDto['permissions'] };
      }
      return {
        module: g.module,
        permissions: (g.permissions ?? []).filter((p) =>
          portal === 'provider' ? isProviderPermission(p.name) : isAdminPermission(p.name)
        ),
      };
    })
    .filter((g) => g.permissions.length > 0);
}

export function filterSelectedPermissions(
  selected: Iterable<string>,
  portal: RolePortal
): string[] {
  return [...selected].filter((name) =>
    portal === 'provider' ? isProviderPermission(name) : isAdminPermission(name)
  );
}

/** ASP.NET may serialize UserType as number (Admin=2, Provider=3) or string name. */
export function matchesRoleType(
  roleType: string | number | null | undefined,
  portal: RolePortal
): boolean {
  if (roleType === null || roleType === undefined) return false;
  if (typeof roleType === 'number') {
    return portal === 'admin' ? roleType === 2 : roleType === 3;
  }
  const normalized = String(roleType).trim().toLowerCase();
  return portal === 'admin' ? normalized === 'admin' : normalized === 'provider';
}

export function filterRolesForPortal(
  roles: RoleListItemDto[] | null | undefined,
  portal: RolePortal
): RoleListItemDto[] {
  return (roles ?? []).filter((r) => matchesRoleType(r.roleType, portal));
}

/** Normalize API permission payloads (handles camelCase / PascalCase). */
export function normalizePermissionGroups(raw: unknown): PermissionGroupDto[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((g: Record<string, unknown>) => {
    const perms = (g['permissions'] ?? g['Permissions'] ?? []) as Record<string, unknown>[];
    return {
      module: String(g['module'] ?? g['Module'] ?? ''),
      permissions: perms.map((p) => ({
        name: String(p['name'] ?? p['Name'] ?? ''),
        action: String(p['action'] ?? p['Action'] ?? ''),
        description: (p['description'] ?? p['Description'] ?? null) as string | null,
      })),
    };
  });
}
