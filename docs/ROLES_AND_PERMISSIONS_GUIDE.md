# Subito Platform Roles & Permissions Guide

The **Subito** platform (a Multi-Vendor Marketplace inspired by Noon, Talabat, and InstaShop) is built on a flexible, fine-grained, and robust authorization architecture. It strictly isolates **Portals**, distinguishes between undeletable **System Super Principals (`IsSystem = true`)** and store/platform **Custom Staff Roles (`IsSystem = false`)**, and incrementally expands permissions module-by-module via an automated seeding engine.

---

## 1. High-Level Architecture & Portals

```mermaid
flowchart TD
    subgraph System [Subito Platform]
        subgraph AdminPortal [1. Admin Portal - Admin Dashboard]
            SuperAdmin["Super Admin<br>(Protected System Account & Role IsSystem=true - 100% Admin Privileges)"]
            AdminRolesCrud["Admin Roles Management (/api/v1/admin/roles)"]
            CustomAdminRoles["Custom Platform Staff Roles<br>(Quality Officer, Provider Manager, Auditor...)"]
            SuperAdmin --> AdminRolesCrud
            AdminRolesCrud --> CustomAdminRoles
        end

        subgraph ProviderPortal [2. Provider Portal - Merchant Dashboard]
            SuperProvider["Super Provider<br>(Store Owner - Protected System Account & Role IsSystem=true)"]
            ProviderRolesCrud["Store Roles Management (/api/v1/provider/roles)"]
            CustomProviderRoles["Custom Store Staff Roles<br>(Cashier, Warehouse Clerk, Branch Supervisor...)"]
            SuperProvider --> ProviderRolesCrud
            ProviderRolesCrud --> CustomProviderRoles
        end

        subgraph ClientPortal [3. Client Portal - Customer Storefront]
            Client["Client<br>(Public Shopper Account - Browse & Purchase)"]
        end
    end

    SuperAdmin -->|"Create, Activate, Deactivate"| SuperProvider
    SuperProvider -->|"Manage Store Staff & Roles"| PlatformCatalog["Store Operations"]
    Client -->|"Browse & Checkout"| PlatformCatalog
    SuperAdmin -->|"Full Platform & Catalog Governance"| PlatformCatalog
```

---

## 2. Protected System Principals (Super Entities)

These principals represent permanent, non-deletable system identities (`IsSystem = true`):

### A) Super Admin (Platform Super Administrator):
* **Who are they?** The supreme administrative and operational authority across the entire Subito platform.
* **Privileges:** Has **all Admin Portal permissions (All Admin Permissions)** permanently and unconditionally (covering all current and future admin modules).
* **Automatic Synchronization:** On application startup, the `DbSeeder` examines `PermissionCatalog.cs` and automatically grants any newly registered admin permission to the `Admin` role.
* **Protection & Security:** Both the Super Admin user account and the `Admin` role are marked as **`IsSystem = true`**. Any deletion attempt (`DELETE`) is immediately blocked by the backend, returning HTTP `409 Conflict` with the localized message: `Role.CannotDeleteSystemUser` or `Role.CannotDeleteSystemRole`.

### B) Super Provider (Merchant / Store Owner):
* **Who are they?** The primary account holder and owner of a merchant store or seller business created by platform administration.
* **Privileges:** Holds **all Provider Portal permissions (All Provider Permissions)** to manage the store profile, branch staff, and product catalog.
* **Automatic Synchronization:** On startup, `DbSeeder` automatically synchronizes the `Provider` role with any newly registered provider permissions (e.g., when future provider modules such as order fulfillment and store inventory management are introduced).
* **Protection & Security:** Both the merchant account and the `Provider` role are marked as **`IsSystem = true`**. Store staff members cannot delete, deactivate, or modify the Super Provider identity.

### C) Client (End Shopper):
* Public shopper role with self-service registration, guarded by the `[RequireClient]` policy.

---

## 3. Custom Staff Roles (Custom Roles)

These roles are dynamically created and configured by administrators in the Admin Dashboard or by store owners in their merchant dashboard via the **Roles Management API** (`IsSystem = false`):

| Custom Role Type | Created By | Permitted Scope | Real-World Examples |
| :--- | :--- | :--- | :--- |
| **Custom Admin Role** | Super Admin in Admin Dashboard | Subset of **Admin Portal** permissions | **Provider Operations Officer:** Has `Providers.Read` & `Providers.Update`<br>**Services Manager:** Has `Services.Read` & `Services.Update` only |
| **Custom Provider Role** | Super Provider in Merchant Dashboard | Subset of **Provider Portal** permissions scoped to that specific store | **Store Cashier:** Can view catalog and process orders<br>**Warehouse Associate:** Can adjust stock levels only |

### Multi-Tenant Store Scoping & Isolation
* Every custom role created by a merchant is automatically bound to that merchant's store ID (`AspNetRoles.ProviderId = currentProvider.Id`).
* Store A cannot inspect, modify, or delete custom roles belonging to Store B.
* Store staff can never view, select, or assign any permission originating from the Admin Portal catalog.

---

## 4. Modular Permissions Principle

Permissions are not defined arbitrarily or hard-coded into single lists. Instead, they **scale incrementally alongside each new domain module in the project**:

1. A central catalog exists in code (`PermissionCatalog.cs`) mapping every permission to its target **Portal** (`UserType.Admin` vs `UserType.Provider`), **Module**, and **Action**.
2. **When adding an Admin Module:**
   * Define standard action verbs: `Read`, `Create`, `Update`, `Delete`.
   * *Example - Provider Management Module:*  
     `Providers.Read` | `Providers.Create` | `Providers.Update` | `Providers.Delete`
   * *Example - Admin Roles Management Module:*  
     `Roles.Read` | `Roles.Create` | `Roles.Update` | `Roles.Delete`
   * *Example - Admin Staff Management Module:*  
     `Admins.Read` | `Admins.Create` | `Admins.Update` | `Admins.Delete`
   * *Example - Marketplace Services Module:*  
     `Services.Read` | `Services.Create` | `Services.Update` | `Services.Delete`
3. **When adding a Provider Module:**
   * Define permissions belonging to the merchant portal:
   * *Example - Store Roles Management Module:*  
     `ProviderRoles.Read` | `ProviderRoles.Create` | `ProviderRoles.Update` | `ProviderRoles.Delete`
   * *Example - Store Staff Management Module:*  
     `ProviderStaff.Read` | `ProviderStaff.Create` | `ProviderStaff.Update` | `ProviderStaff.Delete`
4. **Automated Seeder Synchronization:**
   * Whenever new modules and permissions are added to `PermissionCatalog.cs`, the startup `DbSeeder`:
     * Automatically assigns all Admin permissions to **Super Admin**.
     * Automatically assigns all Provider permissions to **Super Provider**.

---

## 5. API Endpoints Specification

Role and permission governance is partitioned into two dedicated controller surfaces:

---

### I. Admin Dashboard (`/api/v1/admin`)

#### 1. Admin Permissions Checklist:
* **`GET /api/v1/admin/permissions`**  
  * **Authorization:** `[RequireAdmin]` + `[RequirePermission("Roles.Read")]`  
  * **Function:** Returns all Admin portal permissions grouped by module (`Providers`, `Roles`, `ApiKeys`, `Admins`, `Services`) for rendering checkbox trees in admin staff role creation/editing UI.

#### 2. Admin Roles CRUD:
| Method | Route | Required Permission | Responsibility |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/v1/admin/roles` | `Roles.Read` | Paged list of administrative roles with search, user count per role, and assigned permissions. |
| **GET** | `/api/v1/admin/roles/{id}` | `Roles.Read` | Fetches details and assigned permission claims for a specific admin role. |
| **POST** | `/api/v1/admin/roles` | `Roles.Create` | Creates a new custom administrative role with designated admin permissions. |
| **PUT** | `/api/v1/admin/roles/{id}` | `Roles.Update` | Updates role name and synchronizes assigned permission claims (system roles are immutable). |
| **DELETE** | `/api/v1/admin/roles/{id}` | `Roles.Delete` | Deletes a custom admin role (deleting `IsSystem = true` roles is blocked). |

#### 3. Admin Staff Management CRUD:
| Method | Route | Required Permission | Responsibility |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/v1/admin/users` | `Admins.Read` | Paged list of administrative staff members with search, role filtering, and active status filtering. |
| **GET** | `/api/v1/admin/users/{id}` | `Admins.Read` | Fetches details, assigned role, and effective permissions for an administrative staff member. |
| **POST** | `/api/v1/admin/users` | `Admins.Create` | Creates a new administrative user, assigns one admin role, and sends welcome email with credentials & dashboard link. |
| **PUT** | `/api/v1/admin/users/{id}` | `Admins.Update` | Updates staff member personal details and reassigns role (system accounts are protected). |
| **POST** | `/api/v1/admin/users/{id}/set-active` | `Admins.Update` | Activates or deactivates an admin account (`{ "isActive": boolean }`). Cannot deactivate Super Admin. |
| **DELETE** | `/api/v1/admin/users/{id}` | `Admins.Delete` | Deletes a custom administrative staff member (deleting `IsSystem = true` is blocked). |

#### 4. Marketplace Services Management CRUD:
| Method | Route | Required Permission | Responsibility |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/v1/admin/services` | `Services.Read` | Paged list of marketplace services with search and active status filters. |
| **GET** | `/api/v1/admin/services/lookup` | `Services.Read` | Dropdown selector list of active services for provider assignment. |
| **GET** | `/api/v1/admin/services/{id}` | `Services.Read` | Fetches details and all translations for a marketplace service. |
| **POST** | `/api/v1/admin/services` | `Services.Create` | Creates a new marketplace category with multilingual names and descriptions. |
| **PUT** | `/api/v1/admin/services/{id}` | `Services.Update` | Modifies service code, display order, and localized translations. |
| **POST** | `/api/v1/admin/services/{id}/set-active` | `Services.Update` | Toggles service active state (`{ "isActive": boolean }`). |
| **DELETE** | `/api/v1/admin/services/{id}` | `Services.Delete` | Soft deletes service (blocked if linked to active providers). |

---

### II. Provider Dashboard (`/api/v1/provider`)

Managed exclusively from within the **Merchant Dashboard** for their respective store staff:

#### 1. Provider Permissions Checklist:
* **`GET /api/v1/provider/permissions`**  
  * **Authorization:** `[RequireProvider]` + `[RequirePermission("ProviderRoles.Read")]`  
  * **Function:** Returns all permissions available for the store portal (`ProviderRoles`, `ProviderStaff`).

#### 2. Provider Roles CRUD:
| Method | Route | Required Permission | Responsibility |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/v1/provider/roles` | `ProviderRoles.Read` | Paged list of roles belonging strictly to the caller's store (`ProviderId == currentProvider.Id`). |
| **GET** | `/api/v1/provider/roles/{id}` | `ProviderRoles.Read` | Fetches details and permissions for a store staff role belonging to the current merchant. |
| **POST** | `/api/v1/provider/roles` | `ProviderRoles.Create` | Creates a new store role (cashier, warehouse, supervisor) scoped to the current store. |
| **PUT** | `/api/v1/provider/roles/{id}` | `ProviderRoles.Update` | Updates the role name and permissions (Super Provider role cannot be altered). |
| **DELETE** | `/api/v1/provider/roles/{id}` | `ProviderRoles.Delete` | Deletes a custom store role (Super Provider system role cannot be deleted). |

#### 3. Provider Store Staff Management CRUD:
| Method | Route | Required Permission | Responsibility |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/v1/provider/staff` | `ProviderStaff.Read` | Paged list of store staff members belonging exclusively to caller's store. |
| **GET** | `/api/v1/provider/staff/{id}` | `ProviderStaff.Read` | Fetches details, role, and effective permissions for a store staff member. |
| **POST** | `/api/v1/provider/staff` | `ProviderStaff.Create` | Creates a store employee, assigns one store role, and sends welcome email with credentials & dashboard link. |
| **PUT** | `/api/v1/provider/staff/{id}` | `ProviderStaff.Update` | Updates employee details and reassigns role (Super Provider owner account cannot be altered). |
| **POST** | `/api/v1/provider/staff/{id}/set-active` | `ProviderStaff.Update` | Activates or deactivates a store employee account (`{ "isActive": boolean }`). Cannot deactivate Super Provider. |
| **DELETE** | `/api/v1/provider/staff/{id}` | `ProviderStaff.Delete` | Deletes a custom store employee account (deleting `IsSystem = true` is blocked). |

---

## 6. Technical Enforcement & Security Mechanisms

1. **Storage Schema:**
   * Permissions are stored as claims inside `AspNetRoleClaims` (`ClaimType = "permission"`).
   * Roles contain `IsSystem`, `RoleType`, and nullable `ProviderId` foreign key to `Providers`.
   * Users contain `IsSystem` to safeguard root accounts, and nullable `ProviderId` foreign key to `Providers` for store staff multi-tenant scoping.
2. **JWT Claims Flattening:**
   * Upon authentication, the identity service resolves all assigned roles, extracts their associated permission claims, flattens them into a unique set, and embeds them directly into the JWT `permission` array.
   * If `user.ProviderId` is set, `provider_id` is embedded in the JWT claims for store isolation.
3. **Dual-Layer Endpoint Protection:**
   * Endpoints enforce two independent authorization gates:
     ```csharp
     [RequireAdmin] // 1. Validates the portal entry via user_type claim
     [RequirePermission("Roles.Create")] // 2. Validates that the caller holds the exact fine-grained permission
     ```
4. **System Protection Guards (`IsSystem`):**
   * Handlers verify `IsSystem` before processing update or delete actions.
   * If `IsSystem == true`, the mutation is rejected with `ConflictException` and HTTP status `409 Conflict`.

---

## 7. Staff Management Architecture

Staff accounts are provisioned and managed under dedicated endpoints, assigning exactly one role from the portal's registered roles, and dispatching credentials via automated welcome email:

### I. Admin Staff (`/api/v1/admin/users`)
- **Protected by:** `[RequireAdmin]` and `Admins.*` permissions (`Admins.Read`, `Admins.Create`, `Admins.Update`, `Admins.Delete`).
- **Functionality:** Create, list, inspect, update details/role, toggle active status, and delete administrative staff.
- **Root Admin Safeguard:** Root Super Admin (`IsSystem = true`) cannot be deactivated or deleted.
- **Onboarding:** Automatically emails credentials (email, temporary password) and direct Admin Dashboard URL.

### II. Store Staff (`/api/v1/provider/staff`)
- **Protected by:** `[RequireProvider]` and `ProviderStaff.*` permissions (`ProviderStaff.Read`, `ProviderStaff.Create`, `ProviderStaff.Update`, `ProviderStaff.Delete`).
- **Functionality:** Create, list, inspect, update details/role, toggle active status, and delete store staff.
- **Store Tenant Scoping:** Every staff member is bound to `ProviderId == currentProvider.Id`.
- **Store Owner Safeguard:** Store Owner Super Provider (`IsSystem = true`) cannot be deactivated or deleted.
- **Onboarding:** Automatically emails credentials (email, temporary password) and direct Merchant Dashboard URL.
