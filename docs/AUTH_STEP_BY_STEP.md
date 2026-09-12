# Authentication Step-by-Step (Phase 1)

**Companion:** [AUTH_FLOW.md](./AUTH_FLOW.md)

## Before you start

```bash
dotnet run --project src/Enterprise.Api
```

Swagger: `https://localhost:7221/swagger`  
Dev DB is migrated/seeded automatically in Development.  
**Note:** Drop/recreate `EnterpriseDb` if you still have the old custom-auth schema.

With empty `Smtp:Host`, register/login responses include `developmentOtp` so you can complete verify in Swagger without email.

## Admin (email + password)

1. `POST /api/v1/admin/auth/login`
   ```json
   { "email": "admin@enterprise.local", "password": "Admin@12345!" }
   ```
2. Copy `accessToken` → Swagger **Authorize**: `Bearer <token>`
3. `GET /api/v1/admin/profile`
4. Optional: `POST /api/v1/admin/auth/forgot-password` → read OTP from logs → `POST /api/v1/admin/auth/reset-password`
5. Optional: `POST /api/v1/admin/auth/change-password` (while authenticated)

## Client (email + OTP)

1. `POST /api/v1/client/auth/register`
   ```json
   { "email": "user@example.com", "firstName": "Ada", "lastName": "Lovelace" }
   ```
2. Read OTP from logs
3. `POST /api/v1/client/auth/verify-registration`
   ```json
   { "email": "user@example.com", "otp": "123456" }
   ```
4. Later login: `POST /api/v1/client/auth/login` → OTP from logs → `POST /api/v1/client/auth/verify-login`
5. `GET /api/v1/client/profile` with Bearer token

## Policies

- Client routes use `[RequireClient]` (`user_type=Client`)
- Admin routes use `[RequireAdmin]` (`user_type=Admin`)
- Products still use `[RequirePermission(...)]` (Admin role is seeded with product permissions)

## Deferred

Roles CRUD (`nameEn`/`nameAr`), permissions admin APIs, API-key polish — next step.
