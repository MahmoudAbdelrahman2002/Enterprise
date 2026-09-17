# Frontend & AI Integration Guide: Authentication & Profile APIs

This document contains everything a Frontend Engineer or an AI coding assistant needs to implement the authentication system for **Web, Mobile (React Native / Flutter), or Desktop** without having to read backend code or consult developers.

---

## 1. Quick Reference & Postman Collection

- **Postman Collection Path**: `postman/Enterprise_Authentication.postman_collection.json`
  - Fully compatible with Postman v2.1, Insomnia, Thunder Client, and Bruno.
  - Automatically captures and saves tokens (`clientAccessToken`, `clientRefreshToken`, `adminAccessToken`, `providerAccessToken`) and development OTP codes into variables.
- **Base URL**: `http://localhost:5207` (Development)
- **API Versioning**: `v1` in URL path (`/api/v1/...`)
- **Default Dev Admin**: `admin@enterprise.local` / `Admin@12345!`

---

## 2. Global Architecture & Response Envelope

Every request returns an identical JSON envelope:

```typescript
export interface ApiResponse<T = any> {
  success: boolean;       // true for 2xx responses, false for errors
  statusCode: number;    // Standard HTTP status code (200, 201, 400, 401, 403, 404, 409, 429, 500)
  message: string;       // Human-readable localized message (safe to display directly in toasts/alerts)
  errors: string[];      // Array of detailed validation error messages (empty on success)
  data: T | null;        // The payload data or null
  traceId: string;       // Correlation ID for tracking logs with backend
}
```

### Global Headers:
| Header | Value | Description |
|---|---|---|
| `Content-Type` | `application/json` | Required for all POST/PUT requests |
| `Accept-Language` | `en` \| `ar` \| `it` | Translates `message` and all validation `errors` dynamically. Default is `en`. |
| `Authorization` | `Bearer <accessToken>` | Required on protected endpoints |

---

## 3. Core Models & TypeScript Interfaces

```typescript
export type UserType = 'Client' | 'Admin' | 'Provider';

export interface UserDto {
  id: string;            // UUID v4
  email: string;
  firstName: string;
  lastName: string;
  userType: UserType;
  roles: string[];       // e.g. ["Client"] or ["Admin"]
}

export interface AuthResponseDto {
  accessToken: string;              // Short-lived JWT (60m dev / 15m prod)
  accessTokenExpiresAtUtc: string;  // ISO 8601 UTC date
  refreshToken: string;             // Long-lived token (7 days)
  user: UserDto;
}

export interface ProfileDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  userType: UserType;
  emailConfirmed: boolean;
}

export interface OtpSentDto {
  message: string;
  developmentOtp?: string | null;  // Available in Development mode!
}

// --- Roles & Permissions Interfaces ---
export interface PermissionItemDto {
  name: string;        // e.g. "Providers.Read"
  action: string;      // e.g. "Read"
  description?: string | null;
}

export interface PermissionGroupDto {
  module: string;      // e.g. "Providers", "Roles", "Admins", "ProviderStaff", "Services"
  permissions: PermissionItemDto[];
}

export interface RoleListItemDto {
  id: string;          // UUID v4
  name: string;        // e.g. "Catalog Specialist"
  roleType: UserType;  // "Admin" | "Provider"
  providerId: string | null;
  isSystem: boolean;   // true for built-in undeletable roles
  usersCount: number;  // Number of users currently assigned this role
  permissions: string[];
}

export interface RoleDetailDto {
  id: string;
  name: string;
  roleType: UserType;
  providerId: string | null;
  isSystem: boolean;
  usersCount: number;
  permissions: string[];
}

export interface CreateRoleRequest {
  name: string;
  permissions: string[];
}

export interface UpdateRoleRequest {
  name: string;
  permissions: string[];
}

// --- Staff Management Interfaces ---
export interface StaffListItemDto {
  id: string;          // UUID v4
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  roleId: string;
  roleName: string;
  userType: UserType;
  providerId?: string | null;
  isActive: boolean;
  isSystem: boolean;   // true for root Super Admin / Super Provider
}

export interface StaffDetailDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  roleId: string;
  roleName: string;
  permissions: string[];
  userType: UserType;
  providerId?: string | null;
  isActive: boolean;
  isSystem: boolean;
}

export interface CreateStaffRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  password: string;
  roleId: string;
}

export interface UpdateStaffRequest {
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  roleId: string;
}

export interface SetStaffActiveRequest {
  isActive: boolean;
}

// --- Provider & Marketplace Services Interfaces ---
export interface ProviderAdminListItemDto {
  id: string;          // UUID v4
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  companyName: string;
  phoneNumber?: string | null;
  serviceId?: string | null;
  serviceName?: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface ProviderAdminDetailDto {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  companyName: string;
  phoneNumber?: string | null;
  serviceId?: string | null;
  serviceName?: string | null;
  isActive: boolean;
  emailConfirmed: boolean;
  createdAtUtc: string;
  lastModifiedAtUtc?: string | null;
}

export interface CreateProviderRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  companyName: string;
  phoneNumber?: string | null;
  serviceId?: string | null;
}

export interface UpdateProviderRequest {
  firstName: string;
  lastName: string;
  companyName: string;
  phoneNumber?: string | null;
  serviceId?: string | null;
}

export interface LocalizedText {
  en: string;
  it?: string | null;
  ar?: string | null;
}

export interface MarketplaceServiceLookupDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
}

export interface MarketplaceServiceTranslationsDto {
  name: LocalizedText;
  description?: LocalizedText | null;
}

export interface MarketplaceServiceDto {
  id: string;
  code: string;
  isActive: boolean;
  displayOrder: number;
  name: string;
  description?: string | null;
  translations: MarketplaceServiceTranslationsDto;
  createdAtUtc: string;
  lastModifiedAtUtc?: string | null;
}

export interface CreateMarketplaceServiceRequest {
  code: string;
  displayOrder?: number;
  isActive?: boolean;
  name: LocalizedText;
  description?: LocalizedText | null;
}

export interface UpdateMarketplaceServiceRequest {
  code: string;
  displayOrder?: number;
  name: LocalizedText;
  description?: LocalizedText | null;
}
```

---

## 4. Endpoints Matrix

### A. Client Authentication (`/api/v1/client/auth`) — Passwordless OTP

| # | Action | Method | Route | Auth | Rate Limit |
|---|---|---|---|---|---|
| 1 | Register | `POST` | `/api/v1/client/auth/register` | Anonymous | 10 req/min |
| 2 | Verify Registration | `POST` | `/api/v1/client/auth/verify-registration` | Anonymous | 10 req/min |
| 3 | Login (Request OTP) | `POST` | `/api/v1/client/auth/login` | Anonymous | 10 req/min |
| 4 | Verify Login | `POST` | `/api/v1/client/auth/verify-login` | Anonymous | 10 req/min |
| 5 | Social Login | `POST` | `/api/v1/client/auth/external` | Anonymous | 10 req/min |
| 6 | Refresh Token | `POST` | `/api/v1/client/auth/refresh-token` | Anonymous | 10 req/min |
| 7 | Revoke Token (Logout) | `POST` | `/api/v1/client/auth/revoke-token` | Anonymous | 10 req/min |

### B. Client Profile & Security (`/api/v1/client/profile`)

| # | Action | Method | Route | Auth |
|---|---|---|---|---|
| 8 | Get Profile | `GET` | `/api/v1/client/profile` | Bearer (Client) |
| 9 | Update Profile | `PUT` | `/api/v1/client/profile` | Bearer (Client) |
| 10 | Request Change Email | `POST` | `/api/v1/client/profile/change-email/request` | Bearer (Client) |
| 11 | Confirm Change Email | `POST` | `/api/v1/client/profile/change-email/confirm` | Bearer (Client) |

### C. Admin Authentication (`/api/v1/admin/auth`) — Password-Based

| # | Action | Method | Route | Auth | Rate Limit |
|---|---|---|---|---|---|
| 12 | Login | `POST` | `/api/v1/admin/auth/login` | Anonymous | 10 req/min |
| 13 | Forgot Password | `POST` | `/api/v1/admin/auth/forgot-password` | Anonymous | 10 req/min |
| 14 | Reset Password | `POST` | `/api/v1/admin/auth/reset-password` | Anonymous | 10 req/min |
| 15 | Change Password | `POST` | `/api/v1/admin/auth/change-password` | Bearer (Admin) | 10 req/min |
| 16 | Refresh Token | `POST` | `/api/v1/admin/auth/refresh-token` | Anonymous | 10 req/min |
| 17 | Revoke Token (Logout) | `POST` | `/api/v1/admin/auth/revoke-token` | Anonymous | 10 req/min |

### D. Admin Profile & Security (`/api/v1/admin/profile`)

| # | Action | Method | Route | Auth |
|---|---|---|---|---|
| 18 | Get Profile | `GET` | `/api/v1/admin/profile` | Bearer (Admin) |
| 19 | Update Profile | `PUT` | `/api/v1/admin/profile` | Bearer (Admin) |
| 20 | Request Change Email | `POST` | `/api/v1/admin/profile/change-email/request` | Bearer (Admin) |
| 21 | Confirm Change Email | `POST` | `/api/v1/admin/profile/change-email/confirm` | Bearer (Admin) |

### E. Provider Authentication (`/api/v1/provider/auth`) — Password-Based (Admin-provisioned)

Providers are sellers (Subito marketplace). Accounts are created only via Admin Providers CRUD — there is no public register/OTP/social login.

| # | Action | Method | Route | Auth | Rate Limit |
|---|---|---|---|---|---|
| 22 | Login | `POST` | `/api/v1/provider/auth/login` | Anonymous | 10 req/min |
| 23 | Forgot Password | `POST` | `/api/v1/provider/auth/forgot-password` | Anonymous | 10 req/min |
| 24 | Reset Password | `POST` | `/api/v1/provider/auth/reset-password` | Anonymous | 10 req/min |
| 25 | Change Password | `POST` | `/api/v1/provider/auth/change-password` | Bearer (Provider) | 10 req/min |
| 26 | Refresh Token | `POST` | `/api/v1/provider/auth/refresh-token` | Anonymous | 10 req/min |
| 27 | Revoke Token (Logout) | `POST` | `/api/v1/provider/auth/revoke-token` | Anonymous | 10 req/min |

### F. Provider Profile & Security (`/api/v1/provider/profile`)

| # | Action | Method | Route | Auth |
|---|---|---|---|---|
| 28 | Get Profile | `GET` | `/api/v1/provider/profile` | Bearer (Provider) |
| 29 | Update Profile | `PUT` | `/api/v1/provider/profile` | Bearer (Provider) |
| 30 | Request Change Email | `POST` | `/api/v1/provider/profile/change-email/request` | Bearer (Provider) |
| 31 | Confirm Change Email | `POST` | `/api/v1/provider/profile/change-email/confirm` | Bearer (Provider) |

### G. Admin Provider Management (`/api/v1/admin/providers`)

Requires Admin JWT plus `Providers.*` permissions.

| # | Action | Method | Route | Auth |
|---|---|---|---|---|
| 32 | List Providers | `GET` | `/api/v1/admin/providers` | Bearer (Admin) + `Providers.Read` |
| 33 | Get Provider | `GET` | `/api/v1/admin/providers/{id}` | Bearer (Admin) + `Providers.Read` |
| 34 | Create Provider | `POST` | `/api/v1/admin/providers` | Bearer (Admin) + `Providers.Create` |
| 35 | Update Provider | `PUT` | `/api/v1/admin/providers/{id}` | Bearer (Admin) + `Providers.Update` |
| 36 | Set Active | `POST` | `/api/v1/admin/providers/{id}/set-active` | Bearer (Admin) + `Providers.Update` |
| 37 | Delete Provider | `DELETE` | `/api/v1/admin/providers/{id}` | Bearer (Admin) + `Providers.Delete` |

### H. Admin Roles & Permissions (`/api/v1/admin/roles`, `/api/v1/admin/permissions`)

Requires Admin JWT plus `Roles.*` permissions.

| # | Action | Method | Route | Auth |
|---|---|---|---|---|
| 38 | Permissions Catalog | `GET` | `/api/v1/admin/permissions` | Bearer (Admin) + `Roles.Read` |
| 39 | List Admin Roles | `GET` | `/api/v1/admin/roles` | Bearer (Admin) + `Roles.Read` |
| 40 | Get Admin Role | `GET` | `/api/v1/admin/roles/{id}` | Bearer (Admin) + `Roles.Read` |
| 41 | Create Admin Role | `POST` | `/api/v1/admin/roles` | Bearer (Admin) + `Roles.Create` |
| 42 | Update Admin Role | `PUT` | `/api/v1/admin/roles/{id}` | Bearer (Admin) + `Roles.Update` |
| 43 | Delete Admin Role | `DELETE` | `/api/v1/admin/roles/{id}` | Bearer (Admin) + `Roles.Delete` |

### I. Provider Roles & Permissions (`/api/v1/provider/roles`, `/api/v1/provider/permissions`)

Requires Provider JWT plus `ProviderRoles.*` permissions. Scoped to caller's store.

| # | Action | Method | Route | Auth |
|---|---|---|---|---|
| 44 | Store Permissions Catalog | `GET` | `/api/v1/provider/permissions` | Bearer (Provider) + `ProviderRoles.Read` |
| 45 | List Store Roles | `GET` | `/api/v1/provider/roles` | Bearer (Provider) + `ProviderRoles.Read` |
| 46 | Get Store Role | `GET` | `/api/v1/provider/roles/{id}` | Bearer (Provider) + `ProviderRoles.Read` |
| 47 | Create Store Role | `POST` | `/api/v1/provider/roles` | Bearer (Provider) + `ProviderRoles.Create` |
| 48 | Update Store Role | `PUT` | `/api/v1/provider/roles/{id}` | Bearer (Provider) + `ProviderRoles.Update` |
| 49 | Delete Store Role | `DELETE` | `/api/v1/provider/roles/{id}` | Bearer (Provider) + `ProviderRoles.Delete` |

### J. Admin Staff Management (`/api/v1/admin/users`)

Requires Admin JWT plus `Admins.*` permissions.

| # | Action | Method | Route | Auth |
|---|---|---|---|---|
| 50 | List Admin Staff | `GET` | `/api/v1/admin/users` | Bearer (Admin) + `Admins.Read` |
| 51 | Get Admin Staff | `GET` | `/api/v1/admin/users/{id}` | Bearer (Admin) + `Admins.Read` |
| 52 | Create Admin Staff | `POST` | `/api/v1/admin/users` | Bearer (Admin) + `Admins.Create` |
| 53 | Update Admin Staff | `PUT` | `/api/v1/admin/users/{id}` | Bearer (Admin) + `Admins.Update` |
| 54 | Set Active | `POST` | `/api/v1/admin/users/{id}/set-active` | Bearer (Admin) + `Admins.Update` |
| 55 | Delete Admin Staff | `DELETE` | `/api/v1/admin/users/{id}` | Bearer (Admin) + `Admins.Delete` |

### K. Provider Store Staff Management (`/api/v1/provider/staff`)

Requires Provider JWT plus `ProviderStaff.*` permissions. Scoped to caller's store.

| # | Action | Method | Route | Auth |
|---|---|---|---|---|
| 56 | List Store Staff | `GET` | `/api/v1/provider/staff` | Bearer (Provider) + `ProviderStaff.Read` |
| 57 | Get Store Staff | `GET` | `/api/v1/provider/staff/{id}` | Bearer (Provider) + `ProviderStaff.Read` |
| 58 | Create Store Staff | `POST` | `/api/v1/provider/staff` | Bearer (Provider) + `ProviderStaff.Create` |
| 59 | Update Store Staff | `PUT` | `/api/v1/provider/staff/{id}` | Bearer (Provider) + `ProviderStaff.Update` |
| 60 | Set Active | `POST` | `/api/v1/provider/staff/{id}/set-active` | Bearer (Provider) + `ProviderStaff.Update` |
| 61 | Delete Store Staff | `DELETE` | `/api/v1/provider/staff/{id}` | Bearer (Provider) + `ProviderStaff.Delete` |

### L. Admin Marketplace Services (`/api/v1/admin/services`)

Requires Admin JWT plus `Services.*` permissions.

| # | Action | Method | Route | Auth |
|---|---|---|---|---|
| 62 | List Services | `GET` | `/api/v1/admin/services` | Bearer (Admin) + `Services.Read` |
| 63 | Services Lookup | `GET` | `/api/v1/admin/services/lookup` | Bearer (Admin) + `Services.Read` |
| 64 | Get Service by ID | `GET` | `/api/v1/admin/services/{id}` | Bearer (Admin) + `Services.Read` |
| 65 | Create Service | `POST` | `/api/v1/admin/services` | Bearer (Admin) + `Services.Create` |
| 66 | Update Service | `PUT` | `/api/v1/admin/services/{id}` | Bearer (Admin) + `Services.Update` |
| 67 | Set Service Active | `POST` | `/api/v1/admin/services/{id}/set-active` | Bearer (Admin) + `Services.Update` |
| 68 | Delete Service | `DELETE` | `/api/v1/admin/services/{id}` | Bearer (Admin) + `Services.Delete` |

### M. Provider Marketplace Services (`/api/v1/provider/services`)

Requires Provider JWT.

| # | Action | Method | Route | Auth |
|---|---|---|---|---|
| 69 | Provider Services Lookup | `GET` | `/api/v1/provider/services/lookup` | Bearer (Provider) |

**Create Provider body (including optional `serviceId`):**
```json
{
  "email": "seller@example.com",
  "password": "Provider@12345!",
  "firstName": "Sara",
  "lastName": "Seller",
  "companyName": "Sara Electronics",
  "phoneNumber": "+971500000000",
  "serviceId": "7b1c3e4a-9f5a-4b2c-8d1e-3a5f7c9e1b3d"
}
```

**Set-active body:** `{ "isActive": false }` — inactive providers cannot log in.

**Delete:** soft-deletes the store profile and deactivates the Identity user.

---

## 5. Detailed Request & Validation Rules

### 1. `POST /api/v1/client/auth/register`
- **Body**:
  ```json
  {
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe"
  }
  ```
- **Validation Rules**:
  - `email`: Required, valid email format, max 256 chars.
  - `firstName`: Required, non-empty, max 100 chars.
  - `lastName`: Required, non-empty, max 100 chars.
- **Success (`200 OK`)**: Returns `data.developmentOtp` in Development.
- **Errors**: `400` (Validation), `409` (Email already registered).

### 2. `POST /api/v1/client/auth/verify-registration`
- **Body**:
  ```json
  {
    "email": "user@example.com",
    "otp": "482910"
  }
  ```
- **Validation Rules**:
  - `email`: Required, valid email format.
  - `otp`: Required, string length 4 to 10 (standard: 6 digits).
- **Success (`200 OK`)**: Returns `AuthResponseDto` (`accessToken`, `refreshToken`, `user`).
- **Errors**: `401` (Invalid or expired code).

### 3. `POST /api/v1/client/auth/login`
- **Body**:
  ```json
  {
    "email": "user@example.com"
  }
  ```
- **Validation Rules**:
  - `email`: Required, valid email format.
- **Success (`200 OK`)**: Returns `data.developmentOtp` in Development.
- **Anti-Enumeration**: If email is not registered or inactive, still returns `200 OK` with `"If an account exists..."` message.

### 4. `POST /api/v1/client/auth/verify-login`
- **Body**:
  ```json
  {
    "email": "user@example.com",
    "otp": "739104"
  }
  ```
- **Success (`200 OK`)**: Returns `AuthResponseDto`.

### 5. `POST /api/v1/client/auth/external` (Social Login)
- **Body**:
  ```json
  {
    "provider": "Google",
    "idToken": "<raw_jwt_from_google_or_facebook_sdk>"
  }
  ```
- **Validation Rules**:
  - `provider`: Required, allowed: `"Google"` or `"Facebook"` (case-insensitive).
  - `idToken`: Required, max 8192 chars.

### 6. `POST /api/v1/client/auth/refresh-token` (Token Rotation)
- **Body**:
  ```json
  {
    "refreshToken": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
  ```
- **Behavior**: Validates old refresh token, revokes it immediately, and returns **both a new `accessToken` AND a new `refreshToken`**.

### 7. `POST /api/v1/admin/auth/login`
- **Body**:
  ```json
  {
    "email": "admin@enterprise.local",
    "password": "Admin@12345!"
  }
  ```
- **Lockout Policy**: After **5 consecutive failed attempts**, returns `401 Unauthorized` with `"Account is locked due to multiple failed login attempts. Please try again later."` (Locked for 15 minutes).

### 8. `POST /api/v1/admin/auth/reset-password`
- **Body**:
  ```json
  {
    "email": "admin@enterprise.local",
    "otp": "123456",
    "newPassword": "NewAdminPassword@2026!"
  }
  ```
- **Password Complexity Rules** (`newPassword`):
  - Minimum 8 characters.
  - At least 1 uppercase letter (`[A-Z]`).
  - At least 1 lowercase letter (`[a-z]`).
  - At least 1 digit (`[0-9]`).
  - At least 1 special character (`[^a-zA-Z0-9]` e.g. `!@#$%^&*`).

### 9. Roles & Permissions Management (`/admin/roles`, `/admin/permissions`, `/provider/roles`, `/provider/permissions`)

#### A. Fetch Permissions Tree (`GET /api/v1/admin/permissions` or `/api/v1/provider/permissions`)
- **Headers**: `Authorization: Bearer <token>`
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

#### B. Create Role (`POST /api/v1/admin/roles` or `/api/v1/provider/roles`)
- **Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
- **Request Body**:
  ```json
  {
    "name": "Catalog Specialist",
    "permissions": [
      "Providers.Read",
      "Providers.Update",
      "Services.Read"
    ]
  }
  ```
- **Validation Rules**:
  - `name`: Required, trimmed, unique within the portal/store scope.
  - `permissions`: Non-empty array of valid permission strings belonging to the caller's portal.
- **Success (`201 Created`)**: Returns `RoleDetailDto`.
- **Errors**: `400 Bad Request` (Invalid permissions or validation failure), `409 Conflict` (Role name already exists).

#### C. Update Role (`PUT /api/v1/admin/roles/{id}` or `/api/v1/provider/roles/{id}`)
- **Request Body**: Same as Create.
- **Rules**: Cannot update protected system roles (`isSystem: true`). Returns `409 Conflict` (`Role.CannotModifySystemRole`).

#### D. Delete Role (`DELETE /api/v1/admin/roles/{id}` or `/api/v1/provider/roles/{id}`)
- **Rules**: Cannot delete protected system roles (`isSystem: true`). Returns `409 Conflict` (`Role.CannotDeleteSystemRole`).

---

### 10. Staff Management (`/admin/users`, `/provider/staff`)

#### A. List Staff (`GET /api/v1/admin/users` or `/api/v1/provider/staff`)
- **Query Parameters**:
  - `pageNumber`: int (default `1`, min `1`)
  - `pageSize`: int (default `10`, range `1..100`)
  - `searchTerm`: string optional (filters by email, first name, last name, phone)
  - `roleId`: UUID optional (filters by specific assigned role)
  - `isActive`: boolean optional (`true` | `false`)
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
          "id": "c138f512-887e-46a2-9b5f-557ecbe22901",
          "firstName": "Sarah",
          "lastName": "Connor",
          "email": "sarah.connor@enterprise.local",
          "phoneNumber": "+1234567890",
          "roleId": "d558b9f7-66a1-43e5-8278-df096ee656b2",
          "roleName": "Catalog Specialist",
          "userType": "Admin",
          "providerId": null,
          "isActive": true,
          "isSystem": false
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

#### B. Create Staff Member (`POST /api/v1/admin/users` or `/api/v1/provider/staff`)
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
- **Validation Rules**:
  - `firstName`, `lastName`: Required, max 50 chars.
  - `email`: Required, valid email format, max 256 chars, unique across users.
  - `password`: Required, minimum 8 characters.
  - `roleId`: Required, must correspond to an active role in the corresponding portal (and matching caller's store if provider).
- **Backend Actions**:
  - Creates user with `EmailConfirmed = true` and `IsActive = true`.
  - Assigns role in `AspNetUserRoles`.
  - Sends HTML welcome email containing login credentials and direct dashboard login link.
- **Success (`201 Created`)**: Returns `StaffDetailDto`.
- **Errors**: `400 Bad Request` (Validation), `409 Conflict` (Email already exists or role not found).

#### C. Update Staff Member (`PUT /api/v1/admin/users/{id}` or `/api/v1/provider/staff/{id}`)
- **Request Body**:
  ```json
  {
    "firstName": "Sarah",
    "lastName": "Connor-Updated",
    "phoneNumber": "+1234567899",
    "roleId": "d558b9f7-66a1-43e5-8278-df096ee656b2"
  }
  ```
- **Rules**: Cannot modify system accounts (`isSystem: true`).

#### D. Toggle Staff Active Status (`POST /api/v1/admin/users/{id}/set-active` or `/api/v1/provider/staff/{id}/set-active`)
- **Request Body**:
  ```json
  {
    "isActive": false
  }
  ```
- **Rules**: Cannot deactivate protected system accounts (`isSystem: true`). Returns `409 Conflict`.

#### E. Delete Staff Member (`DELETE /api/v1/admin/users/{id}` or `/api/v1/provider/staff/{id}`)
- **Rules**: Cannot delete protected system accounts (`isSystem: true`). Returns `409 Conflict` (`Role.CannotDeleteSystemUser`).

---

## 6. Recommended Frontend Auth Interceptor (Axios Example)

```typescript
import axios from 'axios';

export const apiClient = axios.create({
  baseURL: 'http://localhost:5207/api/v1',
  headers: {
    'Content-Type': 'application/json',
    'Accept-Language': 'en', // Change to 'ar' or 'it' based on user preference
  },
});

// Attach access token
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Automatic token refresh on 401
let isRefreshing = false;
let failedQueue: Array<{ resolve: (token: string) => void; reject: (err: any) => void }> = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) prom.reject(error);
    else prom.resolve(token!);
  });
  failedQueue = [];
};

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return apiClient(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = localStorage.getItem('refreshToken');
      const userType = localStorage.getItem('userType') || 'client';
      const refreshEndpoint =
        userType === 'Admin'
          ? '/admin/auth/refresh-token'
          : userType === 'Provider'
            ? '/provider/auth/refresh-token'
            : '/client/auth/refresh-token';

      try {
        const { data: res } = await axios.post(`http://localhost:5207/api/v1${refreshEndpoint}`, {
          refreshToken,
        });

        const newAccessToken = res.data.accessToken;
        const newRefreshToken = res.data.refreshToken;

        localStorage.setItem('accessToken', newAccessToken);
        localStorage.setItem('refreshToken', newRefreshToken);

        apiClient.defaults.headers.common.Authorization = `Bearer ${newAccessToken}`;
        processQueue(null, newAccessToken);
        return apiClient(originalRequest);
      } catch (refreshErr) {
        processQueue(refreshErr, null);
        localStorage.clear();
        window.location.href = '/login';
        return Promise.reject(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }
    return Promise.reject(error);
  }
);
```

---

## 7. Staff Management Integration (Admin & Store Staff)

Both the Admin Portal and Provider Portal include full staff management modules with role assignment, status toggling, and automated welcome emails with initial credentials.

### Admin Staff Management (`/api/v1/admin/users`)
- **Required Permission**: `Admins.Read` for listing and viewing, `Admins.Create` for creating, `Admins.Update` for updating / setting active, `Admins.Delete` for deleting.
- **Workflow**:
  1. Fetch available administrative roles (`GET /api/v1/admin/roles`).
  2. Create an admin staff member (`POST /api/v1/admin/users`):
     ```json
     {
       "firstName": "Sarah",
       "lastName": "Connor",
       "email": "sarah.connor@enterprise.local",
       "phoneNumber": "+1234567890",
       "password": "AdminStaff@12345!",
       "roleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
     }
     ```
  3. The backend creates the account, assigns the role, and immediately sends an HTML welcome email with credentials and the Admin Dashboard URL.

### Store Staff Management (`/api/v1/provider/staff`)
- **Required Permission**: `ProviderStaff.Read` for listing and viewing, `ProviderStaff.Create` for creating, `ProviderStaff.Update` for updating / setting active, `ProviderStaff.Delete` for deleting.
- **Tenant Isolation**: Operations are strictly scoped to the authenticated provider's store.
- **Workflow**:
  1. Fetch store staff roles (`GET /api/v1/provider/roles`).
  2. Create a store staff member (`POST /api/v1/provider/staff`):
     ```json
     {
       "firstName": "Alex",
       "lastName": "Smith",
       "email": "alex.smith@example.com",
       "phoneNumber": "+1987654321",
       "password": "StoreStaff@12345!",
       "roleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
     }
     ```
  3. The backend creates the employee account under the current store (`ProviderId`), assigns the role, and emails credentials with the Merchant Dashboard URL.

---

### Marketplace Services & Provider Assignment (`/api/v1/admin/services`, `/api/v1/provider/services`)

The Subito platform supports business service categorizations (e.g., **Restaurant**, **Pharmacy**, **Grocery**).

#### 1. Frontend Dropdown Integration (Admin Provider Onboarding)
When creating or editing a Provider in the Admin portal:
1. Call `GET /api/v1/admin/services/lookup` (returns active services with `id`, `code`, `name`, `description` localized based on `Accept-Language`).
2. Populate the **"Service Category"** `<select>` / dropdown with the returned services list.
3. Pass `serviceId: selectedService.id` when calling `POST /api/v1/admin/providers` or `PUT /api/v1/admin/providers/{id}`.
4. The provider detail/list response includes both `serviceId` and `serviceName` (e.g. `"Restaurant"` / `"مطعم"` / `"Ristorante"`).

#### 2. Provider Store View Lookup
In the Provider Dashboard:
- Call `GET /api/v1/provider/services/lookup` (guarded by `Bearer <providerAccessToken>`) to discover available marketplace service categories.

#### 3. Managing Marketplace Services (Admin Portal)
- **Create Service**: `POST /api/v1/admin/services`
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
      "en": "Smartphones, laptops, and gadgets",
      "ar": "الهواتف الذكية وأجهزة الحاسوب والأجهزة الذكية",
      "it": "Smartphone, laptop e gadget"
    }
  }
  ```
- **Update Service**: `PUT /api/v1/admin/services/{id}`
- **Toggle Active**: `POST /api/v1/admin/services/{id}/set-active` with `{ "isActive": boolean }`
- **Delete Service**: `DELETE /api/v1/admin/services/{id}` (rejection guard prevents deleting a category currently in use by active providers)


