import { PermissionGroupDto } from '../models/domain.models';

function group(module: string, moduleLabel: string, items: [string, string, string][]): PermissionGroupDto {
  return {
    module,
    moduleLabel,
    permissions: items.map(([action, name, description]) => ({ name, action, description })),
  };
}

/** Admin portal permissions from PermissionCatalog. Shown even if the API call fails. */
export const ADMIN_PERMISSION_GROUPS: PermissionGroupDto[] = [
  group('Providers', 'Marketplace providers', [
    ['Read', 'Providers.Read', 'View marketplace providers'],
    ['Create', 'Providers.Create', 'Onboard new marketplace providers'],
    ['Update', 'Providers.Update', 'Update provider details and active state'],
    ['Delete', 'Providers.Delete', 'Remove marketplace providers'],
  ]),
  group('Roles', 'Admin roles', [
    ['Read', 'Roles.Read', 'View administrative roles and assigned permissions'],
    ['Create', 'Roles.Create', 'Create administrative roles with permissions'],
    ['Update', 'Roles.Update', 'Modify administrative roles and permissions'],
    ['Delete', 'Roles.Delete', 'Delete administrative custom roles'],
  ]),
  group('Admins', 'Admin staff', [
    ['Read', 'Admins.Read', 'View platform administrative staff'],
    ['Create', 'Admins.Create', 'Create new administrative staff'],
    ['Update', 'Admins.Update', 'Update administrative staff and status'],
    ['Delete', 'Admins.Delete', 'Delete administrative staff'],
  ]),
  group('Services', 'Marketplace services', [
    ['Read', 'Services.Read', 'View marketplace services'],
    ['Create', 'Services.Create', 'Create new marketplace services'],
    ['Update', 'Services.Update', 'Update marketplace services and translations'],
    ['Delete', 'Services.Delete', 'Delete marketplace services'],
  ]),
  group('Clients', 'Clients', [
    ['Read', 'Clients.Read', 'View marketplace clients'],
    ['Update', 'Clients.Update', 'Activate or deactivate marketplace clients'],
  ]),
  group('Orders', 'Platform orders', [
    ['Read', 'Orders.Read', 'View marketplace orders'],
  ]),
  group('ApiKeys', 'API keys', [
    ['Create', 'ApiKeys.Create', 'Generate service API keys'],
  ]),
];

/** Provider portal permissions. Store staff, catalog, and orders only. */
export const PROVIDER_PERMISSION_GROUPS: PermissionGroupDto[] = [
  group('ProviderRoles', 'Store roles', [
    ['Read', 'ProviderRoles.Read', 'View store staff roles'],
    ['Create', 'ProviderRoles.Create', 'Create store staff roles'],
    ['Update', 'ProviderRoles.Update', 'Modify store staff roles'],
    ['Delete', 'ProviderRoles.Delete', 'Delete store staff roles'],
  ]),
  group('ProviderStaff', 'Store staff', [
    ['Read', 'ProviderStaff.Read', 'View store staff members'],
    ['Create', 'ProviderStaff.Create', 'Create store staff members'],
    ['Update', 'ProviderStaff.Update', 'Update store staff and status'],
    ['Delete', 'ProviderStaff.Delete', 'Delete store staff members'],
  ]),
  group('ProviderCategory', 'Categories', [
    ['Read', 'ProviderCategory.Read', 'View store categories'],
    ['Create', 'ProviderCategory.Create', 'Create store categories'],
    ['Update', 'ProviderCategory.Update', 'Update store categories'],
    ['Delete', 'ProviderCategory.Delete', 'Delete store categories'],
  ]),
  group('ProviderProduct', 'Products', [
    ['Read', 'ProviderProduct.Read', 'View store products'],
    ['Create', 'ProviderProduct.Create', 'Create store products'],
    ['Update', 'ProviderProduct.Update', 'Update store products'],
    ['Delete', 'ProviderProduct.Delete', 'Delete store products'],
  ]),
  group('ProviderOrder', 'Orders', [
    ['Read', 'ProviderOrder.Read', 'View store orders'],
    ['Update', 'ProviderOrder.Update', 'Update store order status'],
  ]),
];

export function normalizePermissionGroups(raw: unknown): PermissionGroupDto[] {
  if (!Array.isArray(raw)) return [];
  return raw
    .map((g: Record<string, unknown>) => {
      const perms = (g['permissions'] ?? g['Permissions'] ?? []) as Record<string, unknown>[];
      return {
        module: String(g['module'] ?? g['Module'] ?? ''),
        moduleLabel: String(g['moduleLabel'] ?? g['ModuleLabel'] ?? g['module'] ?? g['Module'] ?? ''),
        permissions: (Array.isArray(perms) ? perms : []).map((p) => ({
          name: String(p['name'] ?? p['Name'] ?? ''),
          action: String(p['action'] ?? p['Action'] ?? ''),
          description: (p['description'] ?? p['Description'] ?? null) as string | null,
        })),
      };
    })
    .filter((g) => g.module && g.permissions.length);
}
