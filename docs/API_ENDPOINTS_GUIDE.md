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
| 24 | `POST` | `/api/v1/admin/providers` | `Providers.Create` | **Create Provider**: Provisions a new merchant user (`UserType.Provider`) with initial credentials and creates their store profile. |
| 25 | `PUT` | `/api/v1/admin/providers/{id}` | `Providers.Update` | **Update Provider**: Modifies the company name, phone number, and account holder's name. |
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

## 8. Admin Products Management
- **Base Route**: `/api/v1/admin/products`
- **Protection**: Requires `Bearer <adminAccessToken>` and fine-grained `Products.*` permissions.

| # | Method | Route | Required Permission | Description & Responsibility |
|---|---|---|---|---|
| 38 | `GET` | `/api/v1/admin/products` | `[RequireAdmin]` | **List Products**: Paged product catalog view supporting search, sorting, and multi-lingual details. |
| 39 | `GET` | `/api/v1/admin/products/{id}` | `[RequireAdmin]` | **Product Details**: Returns the complete product entity including **all available translations** (English, Arabic, Italian). |
| 40 | `POST` | `/api/v1/admin/products` | `Products.Create` | **Create Product**: Adds a new catalog product with SKU, pricing, initial inventory, and multi-lingual translations. |
| 41 | `PUT` | `/api/v1/admin/products/{id}` | `Products.Update` | **Update Product**: Modifies product pricing, descriptions, and localized content. |
| 42 | `POST` | `/api/v1/admin/products/{id}/adjust-stock` | `Products.Update` | **Adjust Inventory**: Atomically increments or decrements inventory quantity (delta). |
| 43 | `DELETE`| `/api/v1/admin/products/{id}` | `Products.Delete` | **Delete Product**: Performs a soft-delete on the product. |

---

## 9. Public Products Catalog
- **Base Route**: `/api/v1/products`
- **Audience**: Public shoppers, web storefront, and mobile applications.

| # | Method | Route | Security | Description & Responsibility |
|---|---|---|---|---|
| 44 | `GET` | `/api/v1/products` | Anonymous | **Browse Catalog**: Lists active products with content automatically localized according to the `Accept-Language` header. |
| 45 | `GET` | `/api/v1/products/{id}` | `Products.Read` | **Product Details**: Returns product information in the requested language (or all translations if requested by an admin). |
| 46 | `POST/PUT/DELETE` | `/api/v1/products/...` | By Permission | Alternative public catalog management endpoints mirroring the admin product endpoints. |

---

## 10. Health Checks & Background Jobs

| Route | Method | Access | Description & Responsibility |
|---|---|---|---|
| `/health/live` | `GET` | Anonymous | **Liveness Probe**: Confirms the API process is alive and responsive (used by Docker / Kubernetes container orchestrators). |
| `/health/ready` | `GET` | Anonymous | **Readiness Probe**: Verifies database connectivity and essential dependency readiness before accepting traffic. |
| `/hangfire` | `GET` | Admin Cookie / Filter | **Hangfire Dashboard**: Interactive web UI for monitoring background jobs (e.g. daily expired refresh token pruning). |

---

## 11. Admin Roles & Permissions
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
