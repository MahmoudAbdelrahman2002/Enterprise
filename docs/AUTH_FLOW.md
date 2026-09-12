# Authentication (Phase 1) — Identity + Client OTP + Admin password + Social login

Separated portals after ASP.NET Core Identity migration.

## Routes

### Client (`/api/v1/client`)

| Method | Path | Auth |
|---|---|---|
| POST | `/auth/register` | Anonymous — sends registration OTP |
| POST | `/auth/verify-registration` | Anonymous — OTP → JWT |
| POST | `/auth/login` | Anonymous — sends login OTP |
| POST | `/auth/verify-login` | Anonymous — OTP → JWT |
| POST | `/auth/external` | Anonymous — Google/Facebook ID token → JWT |
| POST | `/auth/refresh-token` | Anonymous |
| POST | `/auth/revoke-token` | Anonymous |
| GET/PUT | `/profile` | Client JWT |
| POST | `/profile/change-email/request` | Client JWT |
| POST | `/profile/change-email/confirm` | Client JWT |

### Admin (`/api/v1/admin`)

| Method | Path | Auth |
|---|---|---|
| POST | `/auth/login` | Anonymous — email + password |
| POST | `/auth/forgot-password` | Anonymous — sends reset OTP |
| POST | `/auth/reset-password` | Anonymous |
| POST | `/auth/change-password` | Admin JWT |
| POST | `/auth/refresh-token` | Anonymous |
| POST | `/auth/revoke-token` | Anonymous |
| GET/PUT | `/profile` | Admin JWT |
| POST | `/profile/change-email/request\|confirm` | Admin JWT |

## Social login (Google / Facebook)

Mobile or web apps sign in with the provider SDK, then call:

`POST /api/v1/client/auth/external`

```json
{
  "provider": "Google",
  "idToken": "<provider-id-token-or-facebook-access-token>"
}
```

- `provider`: `Google` or `Facebook`
- Google: send the **ID token** (`credential` / `idToken` from Google Sign-In)
- Facebook: send the **user access token** in `idToken` (validated via Graph `debug_token` + `/me`)
- Response: same JWT payload as OTP verify (`accessToken`, `refreshToken`, user)
- New emails create a confirmed Client + link in `AspNetUserLogins`
- Existing Client emails **link** the provider and return JWT
- Admin emails are never linked via this endpoint
- Provider must supply a **verified email**

### Config (`ExternalAuth`)

```json
"ExternalAuth": {
  "Google": {
    "ClientIds": [ "YOUR_GOOGLE_WEB_OR_MOBILE_CLIENT_ID" ]
  },
  "Facebook": {
    "AppId": "YOUR_FACEBOOK_APP_ID",
    "AppSecret": "YOUR_FACEBOOK_APP_SECRET"
  }
}
```

**Google Cloud Console:** create OAuth client IDs for your web/Android/iOS apps. Every Client ID that issues tokens must appear in `ClientIds` (token `aud` must match).

**Facebook Developer:** create an app, enable Facebook Login, request `email` permission. Put App ID + App Secret in config (prefer user secrets for the secret).

## Dev admin

- Email: `admin@enterprise.local`
- Password: `Admin@12345!`

## OTP / email

With empty `Smtp:Host`, OTP emails are logged (and captured in integration tests). Configure SMTP in appsettings for real delivery.

## Policies

- `RequireClient` / `RequireAdmin` via `user_type` claim
- Existing `[RequirePermission]` still used by products

## Next step (not in Phase 1)

Roles module (`nameEn`/`nameAr`), permissions admin APIs, products auth cleanup, API keys polish.

See also [AUTH_CODE_WORKFLOW.md](./AUTH_CODE_WORKFLOW.md) (legacy custom-auth notes may be outdated until rewritten).
