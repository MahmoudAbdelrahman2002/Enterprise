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

**Create body:**
```json
{
  "email": "seller@example.com",
  "password": "Provider@12345!",
  "firstName": "Sara",
  "lastName": "Seller",
  "companyName": "Sara Electronics",
  "phoneNumber": "+971500000000"
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
