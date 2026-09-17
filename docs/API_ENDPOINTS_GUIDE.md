# Subito API Endpoints Guide

This document provides a comprehensive reference for all available API endpoints in the **Subito** platform, detailing each endpoint's HTTP method, route, authentication requirements, required permissions, and functional responsibilities across **Client**, **Provider**, and **Admin** portals.

---

## Unified API Response Format

Every API endpoint returns an identical standardized JSON envelope:

```json
{
  "success": true,
  "statusCode": 200,
  "message": "Localized message based on request language",
  "errors": [],
  "data": { ... },
  "traceId": "0HNOGQC9211AB:00000001"
}
```

- **Core Request Headers**:
  - `Accept-Language`: Selects the culture for messages and validation errors (`en`, `ar`, `it`). Default is `en`.
  - `Authorization`: Transmits the JWT access token in the format: `Bearer <accessToken>`.
  - `Content-Type`: `application/json` for requests with a request body.

---

## 1. Client Authentication
- **Base Route**: `/api/v1/client/auth`
- **Workflow**: Passwordless email OTP verification and Social Login (Google / Facebook).

| # | Method | Route | Security | Description & Responsibility |
|---|---|---|---|---|
| 1 | `POST` | `/api/v1/client/auth/register` | Anonymous | **Initiate Registration**: Accepts client first name, last name, and email; dispatches a 6-digit OTP code to the email. |
| 2 | `POST` | `/api/v1/client/auth/verify-registration` | Anonymous | **Confirm Registration**: Validates the OTP code, activates the account, and issues client JWT Access Token and Refresh Token. |
| 3 | `POST` | `/api/v1/client/auth/login` | Anonymous | **Initiate Login**: Sends an OTP code to the provided email (protected against user enumeration attacks). |
| 4 | `POST` | `/api/v1/client/auth/verify-login` | Anonymous | **Confirm Login**: Validates the OTP code and issues client JWT tokens. |
| 5 | `POST` | `/api/v1/client/auth/external` | Anonymous | **Social Authentication**: Instant sign-in via Google or Facebook using an `idToken`. |
| 6 | `POST` | `/api/v1/client/auth/refresh-token` | Anonymous | **Rotate Refresh Token**: Exchanges an expired access token for a new client access and refresh token pair. |
| 7 | `POST` | `/api/v1/client/auth/revoke-token` | Anonymous | **Sign Out (Logout)**: Revokes the specified client refresh token to invalidate the session. |

---

## 2. Client Profile
- **Base Route**: `/api/v1/client/profile`
- **Protection**: Requires `Bearer <clientAccessToken>` and `UserType.Client`.

| # | Method | Route | Security | Description & Responsibility |
|---|---|---|---|---|
| 8 | `GET` | `/api/v1/client/profile` | Bearer (Client) | **Get Profile**: Fetches the authenticated client's personal details (name, email, account type, confirmation status). |
| 9 | `PUT` | `/api/v1/client/profile` | Bearer (Client) | **Update Profile**: Updates the client's first and last name. |
| 10 | `POST` | `/api/v1/client/profile/change-email/request` | Bearer (Client) | **Request Email Change**: Sends a verification OTP code to the requested new email address. |
| 11 | `POST` | `/api/v1/client/profile/change-email/confirm` | Bearer (Client) | **Confirm Email Change**: Verifies the OTP code and commits the new email to the account. |

---

## 3. Provider Authentication
- **Base Route**: `/api/v1/provider/auth`
- **Workflow**: Email and password authentication. Merchant accounts are **provisioned exclusively from the Admin Dashboard** (no public registration).

| # | Method | Route | Security | Description & Responsibility |
|---|---|---|---|---|
| 12 | `POST` | `/api/v1/provider/auth/login` | Anonymous | **Provider Sign In**: Authenticates provider credentials, verifying account is active (`IsActive = true`) and has `UserType.Provider`. |
| 13 | `POST` | `/api/v1/provider/auth/forgot-password` | Anonymous | **Forgot Password**: Sends a password recovery OTP code to the provider's registered email. |
| 14 | `POST` | `/api/v1/provider/auth/reset-password` | Anonymous | **Reset Password**: Sets a new strong password after verifying the recovery OTP. |
| 15 | `POST` | `/api/v1/provider/auth/change-password` | Bearer (Provider) | **Change Password**: Allows an authenticated provider to change their password by supplying the current and new passwords. |
| 16 | `POST` | `/api/v1/provider/auth/refresh-token` | Anonymous | **Refresh Provider Token**: Exchanges the refresh token for a new provider portal access token. |
| 17 | `POST` | `/api/v1/provider/auth/revoke-token` | Anonymous | **Sign Out**: Revokes the provider's active refresh token. |

---

## 4. Provider Profile
- **Base Route**: `/api/v1/provider/profile`
- **Protection**: Requires `Bearer <providerAccessToken>` and `UserType.Provider`.

| # | Method | Route | Security | Description & Responsibility |
|---|---|---|---|---|
| 18 | `GET` | `/api/v1/provider/profile` | Bearer (Provider) | **Get Provider Profile**: Retrieves the current merchant's account profile data. |
| 19 | `PUT` | `/api/v1/provider/profile` | Bearer (Provider) | **Update Provider Profile**: Updates the first and last name of the provider account holder. |
| 20 | `POST` | `/api/v1/provider/profile/change-email/request` | Bearer (Provider) | **Request Email Change**: Dispatches an OTP verification code to the proposed new email. |
| 21 | `POST` | `/api/v1/provider/profile/change-email/confirm` | Bearer (Provider) | **Confirm Email Change**: Applies the email update upon successful OTP validation. |

---

## 5. Admin Provider Management
- **Base Route**: `/api/v1/admin/providers`
- **Protection**: Requires `Bearer <adminAccessToken>` and fine-grained `Providers.*` permissions.

| # | Method | Route | Required Permission | Description & Responsibility |
|---|---|---|---|---|
| 22 | `GET` | `/api/v1/admin/providers` | `Providers.Read` | **List Providers**: Returns a paged list supporting keyword search (company name, contact) and active status filtering (`isActive`). |
| 23 | `GET` | `/api/v1/admin/providers/{id}` | `Providers.Read` | **Get Provider Details**: Returns comprehensive provider store data joined with the underlying Identity user account. |
| 24 | `POST` | `/api/v1/admin/providers` | `Providers.Create` | **Create Provider**: Provisions a new merchant user (`UserType.Provider`) with initial credentials, creates their store profile, and links their marketplace service category (`serviceId`). |
| 25 | `PUT` | `/api/v1/admin/providers/{id}` | `Providers.Update` | **Update Provider**: Modifies company name, phone number, account holder's name, and assigned marketplace service category (`serviceId`). |
| 26 | `POST` | `/api/v1/admin/providers/{id}/set-active` | `Providers.Update` | **Activate / Deactivate**: Toggles the active status (`isActive`), immediately allowing or revoking access. |
| 27 | `DELETE`| `/api/v1/admin/providers/{id}` | `Providers.Delete` | **Delete Provider**: Soft-deletes the provider entity and disables the underlying user account (system accounts are protected). |

---

## 6. Admin Authentication
- **Base Route**: `/api/v1/admin/auth`
- **Workflow**: Email and password authentication with automatic 15-minute account lockout after 5 consecutive failed attempts.

| # | Method | Route | Security | Description & Responsibility |
|---|---|---|---|---|
| 28 | `POST` | `/api/v1/admin/auth/login` | Anonymous | **Admin Sign In**: Authenticates administrative credentials and issues admin JWT tokens. |
| 29 | `POST` | `/api/v1/admin/auth/forgot-password` | Anonymous | **Forgot Password**: Sends a recovery OTP code to the administrator's email. |
| 30 | `POST` | `/api/v1/admin/auth/reset-password` | Anonymous | **Reset Password**: Sets a new password after verifying the OTP code. |
| 31 | `POST` | `/api/v1/admin/auth/change-password` | Bearer (Admin) | **Change Password**: Updates the password for the currently signed-in administrator. |
| 32 | `POST` | `/api/v1/admin/auth/refresh-token` | Anonymous | **Refresh Admin Token**: Issues a new admin access token using a valid admin refresh token. |
| 33 | `POST` | `/api/v1/admin/auth/revoke-token` | Anonymous | **Sign Out**: Revokes the admin refresh token. |

---

## 7. Admin Profile
- **Base Route**: `/api/v1/admin/profile`
- **Protection**: Requires `Bearer <adminAccessToken>` and `UserType.Admin`.

| # | Method | Route | Security | Description & Responsibility |
|---|---|---|---|---|
| 34 | `GET` | `/api/v1/admin/profile` | Bearer (Admin) | **Get Admin Profile**: Retrieves the current administrator's account profile data. |
| 35 | `PUT` | `/api/v1/admin/profile` | Bearer (Admin) | **Update Admin Profile**: Updates the administrator's first and last name. |
| 36 | `POST` | `/api/v1/admin/profile/change-email/request` | Bearer (Admin) | **Request Email Change**: Sends an OTP code to the proposed new email address. |
| 37 | `POST` | `/api/v1/admin/profile/change-email/confirm` | Bearer (Admin) | **Confirm Email Change**: Commits the new email after OTP verification. |

---

## 8. Health Checks & Background Jobs

| Route | Method | Access | Description & Responsibility |
|---|---|---|---|
| `/health/live` | `GET` | Anonymous | **Liveness Probe**: Confirms the API process is alive and responsive (used by Docker / Kubernetes container orchestrators). |
| `/health/ready` | `GET` | Anonymous | **Readiness Probe**: Verifies database connectivity and essential dependency readiness before accepting traffic. |
| `/hangfire` | `GET` | Admin Cookie / Filter | **Hangfire Dashboard**: Interactive web UI for monitoring background jobs (e.g. daily expired refresh token pruning). |

---

## 9. Admin Roles & Permissions
- **Base Route**: `/api/v1/admin/roles` and `/api/v1/admin/permissions`
- **Protection**: Requires `Bearer <adminAccessToken>` and fine-grained `Roles.*` permissions.

| # | Method | Route | Required Permission | Description & Responsibility |
|---|---|---|---|---|
| 47 | `GET` | `/api/v1/admin/permissions` | `Roles.Read` | **Admin Permissions Catalog**: Returns all Admin portal permissions grouped by module for checkbox tree rendering in admin role UI. |
| 48 | `GET` | `/api/v1/admin/roles` | `Roles.Read` | **List Admin Roles**: Paged view of platform roles with keyword search, user count per role, and assigned permissions. |
| 49 | `GET` | `/api/v1/admin/roles/{id}` | `Roles.Read` | **Get Admin Role**: Fetches detailed configuration and permission claims for an administrative role. |
| 50 | `POST` | `/api/v1/admin/roles` | `Roles.Create` | **Create Admin Role**: Provisions a new custom administrative role with designated admin permissions. |
| 51 | `PUT` | `/api/v1/admin/roles/{id}` | `Roles.Update` | **Update Admin Role**: Updates role title and synchronizes permission claims (system roles are protected). |
| 52 | `DELETE`| `/api/v1/admin/roles/{id}` | `Roles.Delete` | **Delete Admin Role**: Deletes a custom administrative role (deleting `IsSystem = true` roles is blocked). |

---

## 12. Provider Roles & Permissions
- **Base Route**: `/api/v1/provider/roles` and `/api/v1/provider/permissions`
- **Protection**: Requires `Bearer <providerAccessToken>` and fine-grained `ProviderRoles.*` permissions.
- **Tenant Isolation**: Operations are strictly scoped to the authenticated merchant's store (`ProviderId == currentProvider.Id`).

| # | Method | Route | Required Permission | Description & Responsibility |
|---|---|---|---|---|
| 53 | `GET` | `/api/v1/provider/permissions` | `ProviderRoles.Read` | **Store Permissions Catalog**: Returns permissions available for merchant staff roles within the Provider portal. |
| 54 | `GET` | `/api/v1/provider/roles` | `ProviderRoles.Read` | **List Store Roles**: Paged view of roles belonging exclusively to the caller's store. |
| 55 | `GET` | `/api/v1/provider/roles/{id}` | `ProviderRoles.Read` | **Get Store Role**: Fetches details and assigned permissions for a store staff role. |
| 56 | `POST` | `/api/v1/provider/roles` | `ProviderRoles.Create` | **Create Store Role**: Creates a custom role (e.g. Store Cashier, Inventory Lead) scoped to the caller's store. |
| 57 | `PUT` | `/api/v1/provider/roles/{id}` | `ProviderRoles.Update` | **Update Store Role**: Modifies role name and permissions (Super Provider system role is protected). |
| 58 | `DELETE`| `/api/v1/provider/roles/{id}` | `ProviderRoles.Delete` | **Delete Store Role**: Deletes a store staff role (Super Provider system role cannot be deleted). |

---

## 13. Admin Staff Management
- **Base Route**: `/api/v1/admin/users`
- **Protection**: Requires `Bearer <adminAccessToken>` and fine-grained `Admins.*` permissions.

| # | Method | Route | Required Permission | Description & Responsibility |
|---|---|---|---|---|
| 59 | `GET` | `/api/v1/admin/users` | `Admins.Read` | **List Admin Staff**: Paged list of administrative users with search, role filtering, active status filter, and assigned role metadata. |
| 60 | `GET` | `/api/v1/admin/users/{id}` | `Admins.Read` | **Get Admin Staff by ID**: Fetches full details for an administrative staff member, including role and effective permissions. |
| 61 | `POST` | `/api/v1/admin/users` | `Admins.Create` | **Create Admin Staff**: Creates a new admin staff member (name, email, phone, password), assigns one administrative role, and sends welcome email with credentials & dashboard link. |
| 62 | `PUT` | `/api/v1/admin/users/{id}` | `Admins.Update` | **Update Admin Staff**: Updates staff name, phone number, and reassigns role (system accounts are protected). |
| 63 | `POST` | `/api/v1/admin/users/{id}/set-active` | `Admins.Update` | **Set Admin Staff Active/Inactive**: Activates or deactivates an admin account (deactivating system admin is blocked). |
| 64 | `DELETE`| `/api/v1/admin/users/{id}` | `Admins.Delete` | **Delete Admin Staff**: Deletes an administrative staff account (`IsSystem = true` accounts cannot be deleted). |

---

## 14. Provider Store Staff Management
- **Base Route**: `/api/v1/provider/staff`
- **Protection**: Requires `Bearer <providerAccessToken>` and fine-grained `ProviderStaff.*` permissions.
- **Tenant Isolation**: Operations are strictly scoped to the authenticated merchant's store (`ProviderId == currentProvider.Id`).

| # | Method | Route | Required Permission | Description & Responsibility |
|---|---|---|---|---|
| 65 | `GET` | `/api/v1/provider/staff` | `ProviderStaff.Read` | **List Store Staff**: Paged list of store staff members belonging exclusively to the caller's store. |
| 66 | `GET` | `/api/v1/provider/staff/{id}` | `ProviderStaff.Read` | **Get Store Staff by ID**: Fetches full details for a store staff member, including role and permissions. |
| 67 | `POST` | `/api/v1/provider/staff` | `ProviderStaff.Create` | **Create Store Staff**: Creates a store employee (name, email, phone, password), assigns one store role, and sends welcome email with credentials & merchant dashboard link. |
| 68 | `PUT` | `/api/v1/provider/staff/{id}` | `ProviderStaff.Update` | **Update Store Staff**: Updates store staff details and reassigns role (store owner account is protected). |
| 69 | `POST` | `/api/v1/provider/staff/{id}/set-active` | `ProviderStaff.Update` | **Set Store Staff Active/Inactive**: Activates or deactivates a store employee account (deactivating store owner is blocked). |
| 70 | `DELETE`| `/api/v1/provider/staff/{id}` | `ProviderStaff.Delete` | **Delete Store Staff**: Deletes a store employee account (`IsSystem = true` store owner accounts cannot be deleted). |

---

## 15. Admin Marketplace Services Management
- **Base Route**: `/api/v1/admin/services`
- **Protection**: Requires `Bearer <adminAccessToken>` and fine-grained `Services.*` permissions.

| # | Method | Route | Required Permission | Description & Responsibility |
|---|---|---|---|---|
| 71 | `GET` | `/api/v1/admin/services` | `Services.Read` | **List Services**: Paged list of marketplace services with search, active status filter, and display ordering. |
| 72 | `GET` | `/api/v1/admin/services/lookup` | `Services.Read` | **Services Lookup**: Dropdown list of active services for provider onboarding / editing selectors. |
| 73 | `GET` | `/api/v1/admin/services/{id}` | `Services.Read` | **Get Service by ID**: Returns complete service entity including all language translations (EN, AR, IT). |
| 74 | `POST` | `/api/v1/admin/services` | `Services.Create` | **Create Service**: Creates a new business service category with code, display order, active state, and multilingual names/descriptions. |
| 75 | `PUT` | `/api/v1/admin/services/{id}` | `Services.Update` | **Update Service**: Modifies code, display order, and localized translations across supported languages. |
| 76 | `POST` | `/api/v1/admin/services/{id}/set-active` | `Services.Update` | **Set Service Active/Inactive**: Toggles active state (`{ "isActive": boolean }`). |
| 77 | `DELETE`| `/api/v1/admin/services/{id}` | `Services.Delete` | **Delete Service**: Soft deletes a marketplace service (blocked if linked to active providers). |

---

## 16. Provider Marketplace Services
- **Base Route**: `/api/v1/provider/services`
- **Protection**: Requires `Bearer <providerAccessToken>`.

| # | Method | Route | Required Permission | Description & Responsibility |
|---|---|---|---|---|
| 78 | `GET` | `/api/v1/provider/services/lookup` | `[RequireProvider]` | **Provider Services Lookup**: Returns active marketplace service categories for store dashboard views. |

---

## Detailed Endpoint Specifications for New Modules

### Section 11: Admin Roles & Permissions Details

#### 11.1 `GET /api/v1/admin/permissions`
- **Security**: `Bearer <adminAccessToken>` + `Roles.Read`
- **Description**: Returns all Admin portal permissions grouped by module (`Admins`, `Roles`, `Providers`, `Services`, `ApiKeys`).
- **Response (`200 OK`)**:
  ```json
  {
    "success": true,
    "statusCode": 200,
    "message": "Permissions retrieved successfully.",
    "errors": [],
    "data": [
      {
        "module": "Admins",
        "permissions": [
          { "name": "Admins.Read", "action": "Read", "description": "View platform administrative staff" },
          { "name": "Admins.Create", "action": "Create", "description": "Create new administrative staff" },
          { "name": "Admins.Update", "action": "Update", "description": "Update administrative staff and status" },
          { "name": "Admins.Delete", "action": "Delete", "description": "Delete administrative staff" }
        ]
      },
      {
        "module": "Roles",
        "permissions": [
          { "name": "Roles.Read", "action": "Read", "description": "View administrative roles and assigned permissions" },
          { "name": "Roles.Create", "action": "Create", "description": "Create administrative roles with permissions" },
          { "name": "Roles.Update", "action": "Update", "description": "Modify administrative roles and permissions" },
          { "name": "Roles.Delete", "action": "Delete", "description": "Delete administrative custom roles" }
        ]
      }
    ],
    "traceId": "0HNOGQC9211AB:00000001"
  }
  ```

#### 11.2 `GET /api/v1/admin/roles`
- **Security**: `Bearer <adminAccessToken>` + `Roles.Read`
- **Query Parameters**:
  - `pageNumber` (int, default `1`)
  - `pageSize` (int, default `10`, range `1..100`)
  - `searchTerm` (string, optional - filters role name)
- **Response (`200 OK`)**:
  ```json
  {
    "success": true,
    "statusCode": 200,
    "message": "Roles retrieved successfully.",
    "errors": [],
    "data": {
      "items": [
        {
          "id": "e44d372c-2917-4886-90f7-669c20a8dbbc",
          "name": "Admin",
          "roleType": "Admin",
          "providerId": null,
          "isSystem": true,
          "usersCount": 1,
          "permissions": ["Admins.Read", "Admins.Create", "Admins.Update", "Admins.Delete", "Roles.Read", "..."]
        }
      ],
      "totalItems": 1,
      "pageNumber": 1,
      "pageSize": 10,
      "totalPages": 1,
      "hasPreviousPage": false,
      "hasNextPage": false
    },
    "traceId": "0HNOGQC9211AB:00000002"
  }
  ```

#### 11.3 `POST /api/v1/admin/roles`
- **Security**: `Bearer <adminAccessToken>` + `Roles.Create`
- **Request Body**:
  ```json
  {
    "name": "Catalog Supervisor",
    "permissions": [
      "Providers.Read",
      "Providers.Update",
      "Services.Read"
    ]
  }
  ```
- **Validation Rules**: `name` required and unique within Admin scope; `permissions` must be valid Admin portal permissions.
- **Response (`201 Created`)**: Returns `RoleDetailDto`.
- **Errors**: `400 Bad Request` (Invalid permissions), `409 Conflict` (Role name exists).

#### 11.4 `PUT /api/v1/admin/roles/{id}`
- **Security**: `Bearer <adminAccessToken>` + `Roles.Update`
- **Request Body**: Same as Create.
- **Business Rule**: Protected system roles (`isSystem = true`, such as the root `Admin` role) cannot be modified. Returns `409 Conflict`.

#### 11.5 `DELETE /api/v1/admin/roles/{id}`
- **Security**: `Bearer <adminAccessToken>` + `Roles.Delete`
- **Business Rule**: Protected system roles (`isSystem = true`) cannot be deleted. Returns `409 Conflict` (`Role.CannotDeleteSystemRole`).

---

### Section 12: Provider Roles & Permissions Details

#### 12.1 `GET /api/v1/provider/permissions`
- **Security**: `Bearer <providerAccessToken>` + `ProviderRoles.Read`
- **Description**: Returns all permissions available for store staff in the Provider portal (`ProviderRoles`, `ProviderStaff`).

#### 12.2 `GET /api/v1/provider/roles`
- **Security**: `Bearer <providerAccessToken>` + `ProviderRoles.Read`
- **Tenant Scoping**: Automatically restricted to caller's `ProviderId`.
- **Query Parameters**: `pageNumber`, `pageSize`, `searchTerm`.

#### 12.3 `POST /api/v1/provider/roles`
- **Security**: `Bearer <providerAccessToken>` + `ProviderRoles.Create`
- **Request Body**:
  ```json
  {
    "name": "Head Cashier",
    "permissions": [
      "ProviderRoles.Read",
      "ProviderStaff.Read"
    ]
  }
  ```
- **Tenant Scoping**: Newly created role is permanently bound to caller's `ProviderId`.
- **Validation**: Role name must be unique within caller's store. Permissions must be valid for Provider portal.

#### 12.4 `PUT /api/v1/provider/roles/{id}` & `DELETE /api/v1/provider/roles/{id}`
- **Security**: `Bearer <providerAccessToken>` + `ProviderRoles.Update` / `ProviderRoles.Delete`
- **Tenant Scoping**: Verified against caller's `ProviderId`.
- **Protection**: Root `Provider` store owner role (`isSystem = true`) cannot be updated or deleted. Returns `409 Conflict`.

---

### Section 13: Admin Staff Management Details (`/api/v1/admin/users`)

#### 13.1 `GET /api/v1/admin/users`
- **Security**: `Bearer <adminAccessToken>` + `Admins.Read`
- **Query Parameters**:
  - `pageNumber` (int, default `1`)
  - `pageSize` (int, default `10`, range `1..100`)
  - `searchTerm` (string, optional - filters email, name, phone)
  - `roleId` (UUID, optional - filters by assigned role)
  - `isActive` (boolean, optional - filters active / inactive)
- **Response (`200 OK`)**:
  ```json
  {
    "success": true,
    "statusCode": 200,
    "message": "Administrative staff list retrieved successfully.",
    "errors": [],
    "data": {
      "items": [
        {
          "id": "b182fb72-9705-4c01-bf63-c79eb46be481",
          "firstName": "System",
          "lastName": "Administrator",
          "email": "admin@enterprise.local",
          "phoneNumber": null,
          "roleId": "e44d372c-2917-4886-90f7-669c20a8dbbc",
          "roleName": "Admin",
          "userType": "Admin",
          "providerId": null,
          "isActive": true,
          "isSystem": true
        }
      ],
      "totalItems": 1,
      "pageNumber": 1,
      "pageSize": 10,
      "totalPages": 1,
      "hasPreviousPage": false,
      "hasNextPage": false
    },
    "traceId": "0HNOGQC9211AB:00000003"
  }
  ```

#### 13.2 `GET /api/v1/admin/users/{id}`
- **Security**: `Bearer <adminAccessToken>` + `Admins.Read`
- **Response (`200 OK`)**: Returns `StaffDetailDto` including `roleId`, `roleName`, and full list of effective `permissions`.

#### 13.3 `POST /api/v1/admin/users`
- **Security**: `Bearer <adminAccessToken>` + `Admins.Create`
- **Request Body**:
  ```json
  {
    "firstName": "Sarah",
    "lastName": "Connor",
    "email": "sarah.connor@enterprise.local",
    "phoneNumber": "+1234567890",
    "password": "AdminStaff@12345!",
    "roleId": "d558b9f7-66a1-43e5-8278-df096ee656b2"
  }
  ```
- **Validation**:
  - `firstName`, `lastName`: Required, max 50 chars.
  - `email`: Required, valid email format, max 256 chars, unique.
  - `password`: Required, min 8 chars.
  - `roleId`: Required, must belong to Admin portal scope.
- **Workflow**:
  1. Validates unique email and admin role existence.
  2. Creates user with `EmailConfirmed = true`, `IsActive = true`, and hashes password.
  3. Assigns the role to user.
  4. Dispatches an HTML welcome email with initial credentials and direct link to Admin Dashboard.
  5. If email dispatch fails, user creation is automatically rolled back.
- **Response (`201 Created`)**: Returns `StaffDetailDto`.

#### 13.4 `PUT /api/v1/admin/users/{id}`
- **Security**: `Bearer <adminAccessToken>` + `Admins.Update`
- **Request Body**:
  ```json
  {
    "firstName": "Sarah",
    "lastName": "Connor-Updated",
    "phoneNumber": "+1234567899",
    "roleId": "d558b9f7-66a1-43e5-8278-df096ee656b2"
  }
  ```
- **Business Rule**: Modifying system admin (`isSystem = true`) is prohibited.

#### 13.5 `POST /api/v1/admin/users/{id}/set-active`
- **Security**: `Bearer <adminAccessToken>` + `Admins.Update`
- **Request Body**:
  ```json
  {
    "isActive": false
  }
  ```
- **Business Rule**: Deactivating root Super Admin (`isSystem = true`) is prohibited. Returns `409 Conflict`.

#### 13.6 `DELETE /api/v1/admin/users/{id}`
- **Security**: `Bearer <adminAccessToken>` + `Admins.Delete`
- **Business Rule**: Deleting root Super Admin (`isSystem = true`) is prohibited. Returns `409 Conflict` (`Role.CannotDeleteSystemUser`).

---

### Section 14: Provider Store Staff Management Details (`/api/v1/provider/staff`)

#### 14.1 `GET /api/v1/provider/staff`
- **Security**: `Bearer <providerAccessToken>` + `ProviderStaff.Read`
- **Tenant Scoping**: Automatically scoped to caller's `ProviderId`.
- **Query Parameters**: `pageNumber`, `pageSize`, `searchTerm`, `roleId`, `isActive`.

#### 14.2 `GET /api/v1/provider/staff/{id}`
- **Security**: `Bearer <providerAccessToken>` + `ProviderStaff.Read`
- **Tenant Scoping**: Can only access staff members belonging to caller's store.

#### 14.3 `POST /api/v1/provider/staff`
- **Security**: `Bearer <providerAccessToken>` + `ProviderStaff.Create`
- **Request Body**:
  ```json
  {
    "firstName": "Alex",
    "lastName": "Smith",
    "email": "alex.smith@example.com",
    "phoneNumber": "+1987654321",
    "password": "StoreStaff@12345!",
    "roleId": "a901f4c3-18e2-4bd5-9854-ce123a456789"
  }
  ```
- **Validation**:
  - `roleId` must belong to caller's store (`ProviderId == currentProvider.Id`).
  - `email` must be unique across the platform.
- **Workflow**:
  1. Creates staff account bound to `ProviderId`.
  2. Assigns store role.
  3. Sends HTML welcome email containing login credentials and Merchant Dashboard login URL.
  4. Automatic rollback if email fails.

#### 14.4 `PUT /api/v1/provider/staff/{id}`
- **Security**: `Bearer <providerAccessToken>` + `ProviderStaff.Update`
- **Tenant Scoping**: Verified against caller's `ProviderId`.
- **Protection**: Cannot modify store owner account (`isSystem = true`).

#### 14.5 `POST /api/v1/provider/staff/{id}/set-active`
- **Security**: `Bearer <providerAccessToken>` + `ProviderStaff.Update`
- **Request Body**: `{ "isActive": false }`
- **Protection**: Cannot deactivate store owner account (`isSystem = true`). Returns `409 Conflict`.

#### 14.6 `DELETE /api/v1/provider/staff/{id}`
- **Security**: `Bearer <providerAccessToken>` + `ProviderStaff.Delete`
- **Protection**: Cannot delete store owner account (`isSystem = true`). Returns `409 Conflict` (`Role.CannotDeleteSystemUser`).

---

### Section 15: Admin Marketplace Services Details

#### 15.1 `GET /api/v1/admin/services`
- **Security**: `Bearer <adminAccessToken>` + `Services.Read`
- **Query Parameters**:
  - `pageNumber` (int, default `1`)
  - `pageSize` (int, default `10`)
  - `searchTerm` (string, optional - filters service code or localized name)
  - `isActive` (bool, optional)
- **Response (`200 OK`)**:
  ```json
  {
    "success": true,
    "statusCode": 200,
    "message": "Marketplace services retrieved successfully.",
    "errors": [],
    "data": {
      "items": [
        {
          "id": "7b1c3e4a-9f5a-4b2c-8d1e-3a5f7c9e1b3d",
          "code": "restaurant",
          "isActive": true,
          "displayOrder": 1,
          "name": "Restaurant",
          "description": "Food, dining, and beverage providers",
          "translations": {
            "name": { "en": "Restaurant", "ar": "مطعم", "it": "Ristorante" },
            "description": { "en": "Food, dining, and beverage providers", "ar": "مزودو خدمات الطعام والمطاعم والمشروبات", "it": "Fornitori di ristorazione, cibo e bevande" }
          },
          "createdAtUtc": "2026-09-16T07:45:00Z",
          "lastModifiedAtUtc": null
        }
      ],
      "pageNumber": 1,
      "pageSize": 10,
      "totalCount": 3,
      "totalPages": 1,
      "hasPreviousPage": false,
      "hasNextPage": false
    },
    "traceId": "0HNOGQC9211AB:00000001"
  }
  ```

#### 15.2 `GET /api/v1/admin/services/lookup`
- **Security**: `Bearer <adminAccessToken>` + `Services.Read`
- **Description**: Lightweight dropdown lookup list of active services localized to caller's `Accept-Language`.
- **Response (`200 OK`)**:
  ```json
  {
    "success": true,
    "statusCode": 200,
    "message": "Marketplace services retrieved successfully.",
    "errors": [],
    "data": [
      {
        "id": "7b1c3e4a-9f5a-4b2c-8d1e-3a5f7c9e1b3d",
        "code": "restaurant",
        "name": "Restaurant",
        "description": "Food, dining, and beverage providers"
      },
      {
        "id": "8c2d4e5b-0a6b-5c3d-9e2f-4b6a8d0f2c4e",
        "code": "pharmacy",
        "name": "Pharmacy",
        "description": "Medications, cosmetics, and medical supplies"
      },
      {
        "id": "9d3e5f6c-1b7c-6d4e-0f3a-5c7b9e1a3d5f",
        "code": "grocery",
        "name": "Grocery",
        "description": "Supermarket, fresh food, and daily essentials"
      }
    ],
    "traceId": "0HNOGQC9211AB:00000001"
  }
  ```

#### 15.3 `GET /api/v1/admin/services/{id}`
- **Security**: `Bearer <adminAccessToken>` + `Services.Read`
- **Response (`200 OK`)**: Full service entity with translations dictionary.

#### 15.4 `POST /api/v1/admin/services`
- **Security**: `Bearer <adminAccessToken>` + `Services.Create`
- **Request Body**:
  ```json
  {
    "code": "electronics",
    "displayOrder": 4,
    "isActive": true,
    "name": {
      "en": "Consumer Electronics",
      "ar": "الأجهزة الإلكترونية",
      "it": "Elettronica di consumo"
    },
    "description": {
      "en": "Smartphones, laptops, and smart gadgets",
      "ar": "الهواتف الذكية وأجهزة الحاسوب والأجهزة الذكية",
      "it": "Smartphone, laptop e gadget intelligenti"
    }
  }
  ```
- **Validation**:
  - `code`: Required, max 100 characters, alphanumeric with hyphens/underscores, unique across non-deleted services.
  - `name`: Required with at least English (`en`) text provided.
  - `displayOrder`: Greater than or equal to 0.

#### 15.5 `PUT /api/v1/admin/services/{id}`
- **Security**: `Bearer <adminAccessToken>` + `Services.Update`
- **Request Body**: Same shape as Create without `isActive`.

#### 15.6 `POST /api/v1/admin/services/{id}/set-active`
- **Security**: `Bearer <adminAccessToken>` + `Services.Update`
- **Request Body**: `{ "isActive": false }`

#### 15.7 `DELETE /api/v1/admin/services/{id}`
- **Security**: `Bearer <adminAccessToken>` + `Services.Delete`
- **Safety Rule**: If any active provider is assigned to this service, returns `409 Conflict` with `Service.HasLinkedProviders` error.

---

### Section 16: Provider Marketplace Services Details

#### 16.1 `GET /api/v1/provider/services/lookup`
- **Security**: `Bearer <providerAccessToken>`
- **Description**: Returns all active marketplace services localized to current user's language for display in store portal.
- **Response (`200 OK`)**: Array of `{ "id": string, "code": string, "name": string, "description": string? }`.
