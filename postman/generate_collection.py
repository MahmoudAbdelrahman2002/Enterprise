#!/usr/bin/env python3
"""Generate the Subito Marketplace Postman collection and environments."""
from __future__ import annotations

import json
from pathlib import Path
from uuid import uuid4

AZURE_URL = "https://subito-api-hda8ggdbhehqa9dk.italynorth-01.azurewebsites.net"
LOCAL_URL = "http://localhost:5207"
BLOB_PLACEHOLDER = "https://<your-storage-account>.blob.core.windows.net"
OUT = Path(__file__).resolve().parent

ADMIN_PERMS = [
    "Admins.Read", "Admins.Create", "Admins.Update", "Admins.Delete",
    "Roles.Read", "Roles.Create", "Roles.Update", "Roles.Delete",
    "Providers.Read", "Providers.Create", "Providers.Update", "Providers.Delete",
    "Services.Read", "Services.Create", "Services.Update", "Services.Delete",
    "Clients.Read", "Clients.Update",
    "Orders.Read",
    "ApiKeys.Create",
]
PROVIDER_PERMS = [
    "ProviderRoles.Read", "ProviderRoles.Create", "ProviderRoles.Update", "ProviderRoles.Delete",
    "ProviderStaff.Read", "ProviderStaff.Create", "ProviderStaff.Update", "ProviderStaff.Delete",
    "ProviderCategory.Read", "ProviderCategory.Create", "ProviderCategory.Update", "ProviderCategory.Delete",
    "ProviderProduct.Read", "ProviderProduct.Create", "ProviderProduct.Update", "ProviderProduct.Delete",
]

IMAGE_RULES = """**Image upload (multipart/form-data)**
| Field | Required | Validation |
|---|---|---|
| `file` | **Yes** on upload endpoints | Form field name **must** be `file`. JPEG / JPG / PNG / WebP (`image/jpeg`, `image/jpg`, `image/png`, `image/webp`). Max **2 MB**. Request size limit **3 MB**. Empty file is rejected. |

Returned `imageUrl` is a **public Azure Blob** URL:
`{{blobPublicBaseUrl}}/{{mediaContainer}}/{folder}/{guid}{ext}`

Folders:
- Provider logo: `providers/{providerId}/logo/`
- Category: `providers/{providerId}/categories/{categoryId}/`
- Product: `providers/{providerId}/products/{productId}/`
- Marketplace service: `services/{serviceId}/`

Images are **optional** on create/update JSON (there is no image field in those bodies). Upload after the entity exists."""


def md_table(rows: list[tuple[str, str, str, str]]) -> str:
    lines = [
        "| Field | Required | Type | Validation / notes |",
        "|---|---|---|---|",
    ]
    for name, req, typ, note in rows:
        lines.append(f"| `{name}` | {req} | {typ} | {note} |")
    return "\n".join(lines)


def envelope(status: int, message: str, data: str, success: bool = True) -> str:
    errors = "[]" if success else f'["{message}"]'
    return (
        "{\n"
        f'  "success": {str(success).lower()},\n'
        f'  "statusCode": {status},\n'
        f'  "message": {json.dumps(message)},\n'
        f'  "errors": {errors},\n'
        f'  "data": {data},\n'
        '  "traceId": "00-abc123def456-0123456789abcdef-00"\n'
        "}"
    )


AUTH_EXAMPLE = """{
      "accessToken": "<jwt>",
      "accessTokenExpiresAtUtc": "2026-09-19T12:00:00Z",
      "refreshToken": "<opaque-refresh-token>",
      "user": {
        "id": "b182fb72-9705-4c01-bf63-c79eb46be481",
        "email": "admin@enterprise.local",
        "firstName": "System",
        "lastName": "Administrator",
        "userType": "Admin",
        "roles": ["Admin"]
      }
    }"""

PROFILE_EXAMPLE = """{
      "id": "b182fb72-9705-4c01-bf63-c79eb46be481",
      "email": "admin@enterprise.local",
      "firstName": "System",
      "lastName": "Administrator",
      "userType": "Admin",
      "emailConfirmed": true
    }"""


def save_tokens(prefix: str) -> list[str]:
    return [
        "const res = pm.response.json();",
        "if (res.success && res.data) {",
        f"    if (res.data.accessToken) pm.collectionVariables.set('{prefix}AccessToken', res.data.accessToken);",
        f"    if (res.data.refreshToken) pm.collectionVariables.set('{prefix}RefreshToken', res.data.refreshToken);",
        "    if (res.data.user && res.data.user.id) pm.collectionVariables.set('" + prefix + "UserId', res.data.user.id);",
        "}",
        "pm.test('HTTP success envelope', function () { pm.expect(res.success).to.eql(true); });",
    ]


def save_otp(var: str = "clientOtp") -> list[str]:
    return [
        "const res = pm.response.json();",
        "if (res.success && res.data && res.data.developmentOtp) {",
        f"    pm.collectionVariables.set('{var}', res.data.developmentOtp);",
        "}",
        "pm.test('OTP request accepted', function () { pm.expect(pm.response.code).to.be.oneOf([200, 201]); });",
    ]


def save_id(var: str) -> list[str]:
    return [
        "const res = pm.response.json();",
        f"if (res.success && res.data && res.data.id) pm.collectionVariables.set('{var}', res.data.id);",
        "pm.test('Succeeded', function () { pm.expect(res.success).to.eql(true); });",
    ]


def ok_test() -> list[str]:
    return [
        "const res = pm.response.json();",
        "pm.test('Envelope success', function () { pm.expect(res.success).to.eql(true); });",
    ]


def header_json(auth_var: str | None = None) -> list[dict]:
    headers = [
        {"key": "Accept-Language", "value": "{{lang}}", "description": "REQUIRED culture: `en` (default), `ar`, `it`. Localizes `message` and validation errors."},
        {"key": "Content-Type", "value": "application/json", "description": "JSON bodies only. Omit for multipart image uploads (Postman sets `multipart/form-data`)."},
    ]
    if auth_var:
        headers.append({
            "key": "Authorization",
            "value": f"Bearer {{{{{auth_var}}}}}",
            "description": f"JWT access token for this portal. Run the matching Login request first so `{auth_var}` is populated.",
        })
    return headers


def header_file(auth_var: str) -> list[dict]:
    return [
        {"key": "Accept-Language", "value": "{{lang}}", "description": "REQUIRED culture: `en`, `ar`, `it`."},
        {
            "key": "Authorization",
            "value": f"Bearer {{{{{auth_var}}}}}",
            "description": f"JWT access token. Run Login first so `{auth_var}` is populated.",
        },
    ]


def url_obj(path: str, query: list[dict] | None = None) -> dict:
    raw = "{{baseUrl}}" + path
    if query:
        enabled = [q for q in query if not q.get("disabled")]
        if enabled:
            raw += "?" + "&".join(f"{q['key']}={q.get('value','')}" for q in enabled)
    segs = [s for s in path.strip("/").split("/") if s]
    obj: dict = {"raw": raw, "host": ["{{baseUrl}}"], "path": segs}
    if query:
        obj["query"] = query
    return obj


def example(name: str, status: int, body: str, original: dict) -> dict:
    return {
        "name": name,
        "originalRequest": original,
        "status": "OK" if status < 400 else "Error",
        "code": status,
        "_postman_previewlanguage": "json",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "body": body,
    }


def request_item(
    name: str,
    method: str,
    path: str,
    description: str,
    *,
    auth_var: str | None = None,
    body: str | None = None,
    query: list[dict] | None = None,
    tests: list[str] | None = None,
    file_upload: bool = False,
    example_body: str | None = None,
    example_status: int = 200,
) -> dict:
    headers = header_file(auth_var) if file_upload else header_json(auth_var if auth_var else None)
    req: dict = {
        "method": method,
        "header": headers,
        "url": url_obj(path, query),
        "description": description,
    }
    if file_upload:
        req["body"] = {
            "mode": "formdata",
            "formdata": [
                {
                    "key": "file",
                    "type": "file",
                    "src": [],
                    "description": (
                        "REQUIRED. Multipart field name must be `file`. "
                        "Allowed: JPEG / PNG / WebP. Max 2 MB. Select any local image in Postman."
                    ),
                }
            ],
        }
    elif body is not None:
        req["body"] = {
            "mode": "raw",
            "raw": body,
            "options": {"raw": {"language": "json"}},
        }

    item: dict = {"name": name, "request": req}
    events = []
    if tests:
        events.append({
            "listen": "test",
            "script": {"type": "text/javascript", "exec": tests},
        })
    if events:
        item["event"] = events
    if example_body:
        item["response"] = [
            example(f"{name} — example", example_status, example_body, req)
        ]
    return item


def folder(name: str, description: str, items: list[dict]) -> dict:
    return {"name": name, "description": description, "item": items}


def q(key: str, value: str, desc: str, disabled: bool = False) -> dict:
    d = {"key": key, "value": value, "description": desc}
    if disabled:
        d["disabled"] = True
    return d


PAGING = [
    q("pageNumber", "1", "OPTIONAL. Page index, 1-based. Default `1`. Must be >= 1."),
    q("pageSize", "20", "OPTIONAL. Page size. Default `20`. Values <= 0 become 20; values > 100 are capped at 100."),
]


def json_pretty(obj) -> str:
    return json.dumps(obj, indent=2, ensure_ascii=False)


# ---------------------------------------------------------------------------
# Folders
# ---------------------------------------------------------------------------

health = folder(
    "00. Health",
    "Anonymous probes used by Azure App Service / Kubernetes. Not wrapped in the API envelope.",
    [
        request_item(
            "Liveness",
            "GET",
            "/health/live",
            """Confirms the API process is running. No auth. No dependency checks.

**Response:** `Healthy` (plain text) with HTTP 200.""",
            example_body="Healthy",
        ),
        request_item(
            "Readiness",
            "GET",
            "/health/ready",
            """Confirms the API can accept traffic (database tagged `ready`). No auth.

**Response:** `Healthy` (plain text) with HTTP 200, or 503 if SQL is unreachable.""",
            example_body="Healthy",
        ),
    ],
)

client_auth = folder(
    "01. Client Auth  /api/v1/client/auth",
    """Passwordless email OTP + Google/Facebook. Anonymous. Rate limit: **5 requests / minute / IP**.

OTP: 6 digits, 10 minutes, max 5 attempts. On Azure, `developmentOtp` is usually omitted (code is emailed).""",
    [
        request_item(
            "Register (request OTP)",
            "POST",
            "/api/v1/client/auth/register",
            f"""Starts client registration and emails a 6-digit OTP.

{md_table([
    ('email', 'Required', 'string', 'Valid email, max 256. Must be unique. 409 if already registered.'),
    ('firstName', 'Required', 'string', 'Max 100.'),
    ('lastName', 'Required', 'string', 'Max 100.'),
])}

**Success 200** `data.message` + optional `data.developmentOtp` (dev only).
**409** email already exists.""",
            body=json_pretty({
                "email": "{{clientEmail}}",
                "firstName": "John",
                "lastName": "Doe",
            }),
            tests=save_otp("clientOtp"),
            example_body=envelope(200, "Registration started. Check your email for the verification code.",
                                  '{\n      "message": "Registration started. Check your email for the verification code.",\n      "developmentOtp": "123456"\n    }'),
        ),
        request_item(
            "Verify registration",
            "POST",
            "/api/v1/client/auth/verify-registration",
            f"""Confirms OTP, activates the client, issues JWT tokens.

{md_table([
    ('email', 'Required', 'string', 'Same email used in register.'),
    ('otp', 'Required', 'string', 'Length 4–10. Default issued length is 6 digits.'),
])}

**401** invalid/expired OTP.""",
            body=json_pretty({"email": "{{clientEmail}}", "otp": "{{clientOtp}}"}),
            tests=save_tokens("client"),
            example_body=envelope(200, "Registration verified successfully.", AUTH_EXAMPLE.replace("Admin", "Client").replace("admin@enterprise.local", "john.doe@example.com")),
        ),
        request_item(
            "Login (request OTP)",
            "POST",
            "/api/v1/client/auth/login",
            f"""Sends a login OTP if the email exists (anti-enumeration: always looks successful).

{md_table([
    ('email', 'Required', 'string', 'Valid email format.'),
])}""",
            body=json_pretty({"email": "{{clientEmail}}"}),
            tests=save_otp("clientOtp"),
            example_body=envelope(200, "If an account exists, a verification code has been sent.",
                                  '{\n      "message": "If an account exists, a verification code has been sent.",\n      "developmentOtp": null\n    }'),
        ),
        request_item(
            "Verify login",
            "POST",
            "/api/v1/client/auth/verify-login",
            f"""Validates the login OTP and issues client JWT tokens.

{md_table([
    ('email', 'Required', 'string', 'Same email used in login.'),
    ('otp', 'Required', 'string', 'Length 4–10 (6 digits issued).'),
])}""",
            body=json_pretty({"email": "{{clientEmail}}", "otp": "{{clientOtp}}"}),
            tests=save_tokens("client"),
        ),
        request_item(
            "Social login (Google / Facebook)",
            "POST",
            "/api/v1/client/auth/external",
            f"""Instant sign-in. Creates the client if needed.

{md_table([
    ('provider', 'Required', 'string', 'Must be `Google` or `Facebook` (case-insensitive).'),
    ('idToken', 'Required', 'string', 'Provider ID token / access token. Max 8192. Google: ID token. Facebook: access token. Email on the social profile must be verified.'),
])}

**401** invalid token. **409** email belongs to a non-client account.""",
            body=json_pretty({
                "provider": "Google",
                "idToken": "<paste-google-id-token-or-facebook-access-token>",
            }),
            tests=save_tokens("client"),
        ),
        request_item(
            "Refresh token",
            "POST",
            "/api/v1/client/auth/refresh-token",
            f"""Rotates the client refresh token. Must be a **Client** refresh token.

{md_table([
    ('refreshToken', 'Required', 'string', 'Opaque token from login/verify. 401 if revoked/expired/wrong portal.'),
])}""",
            body=json_pretty({"refreshToken": "{{clientRefreshToken}}"}),
            tests=save_tokens("client"),
        ),
        request_item(
            "Revoke token (logout)",
            "POST",
            "/api/v1/client/auth/revoke-token",
            f"""Invalidates the refresh token (sign out).

{md_table([
    ('refreshToken', 'Required', 'string', 'Token to revoke. 404 if not found.'),
])}""",
            body=json_pretty({"refreshToken": "{{clientRefreshToken}}"}),
            tests=ok_test(),
            example_body=envelope(200, "Token revoked successfully.", "null"),
        ),
    ],
)

client_profile = folder(
    "02. Client Profile  /api/v1/client/profile",
    "Requires `Bearer {{clientAccessToken}}` and `UserType.Client`.",
    [
        request_item(
            "Get profile",
            "GET",
            "/api/v1/client/profile",
            "Returns the authenticated client's name, email, userType, emailConfirmed.",
            auth_var="clientAccessToken",
            tests=ok_test(),
            example_body=envelope(200, "Profile retrieved successfully.", PROFILE_EXAMPLE.replace("Admin", "Client")),
        ),
        request_item(
            "Update profile",
            "PUT",
            "/api/v1/client/profile",
            f"""{md_table([
    ('firstName', 'Required', 'string', 'Max 100.'),
    ('lastName', 'Required', 'string', 'Max 100.'),
])}""",
            auth_var="clientAccessToken",
            body=json_pretty({"firstName": "John", "lastName": "Doe"}),
            tests=ok_test(),
        ),
        request_item(
            "Request email change",
            "POST",
            "/api/v1/client/profile/change-email/request",
            f"""Sends OTP to the **new** email.

{md_table([
    ('newEmail', 'Required', 'string', 'Valid email, max 256. Must not already be used. 409 if taken.'),
])}""",
            auth_var="clientAccessToken",
            body=json_pretty({"newEmail": "john.new@example.com"}),
            tests=ok_test(),
        ),
        request_item(
            "Confirm email change",
            "POST",
            "/api/v1/client/profile/change-email/confirm",
            f"""{md_table([
    ('newEmail', 'Required', 'string', 'Must match the requested new email.'),
    ('otp', 'Required', 'string', 'Length 4–10. 401 if invalid/expired.'),
])}""",
            auth_var="clientAccessToken",
            body=json_pretty({"newEmail": "john.new@example.com", "otp": "{{clientOtp}}"}),
            tests=ok_test(),
        ),
    ],
)

provider_auth = folder(
    "03. Provider Auth  /api/v1/provider/auth",
    """Email + password. No public registration — Admin creates merchants. Rate limit **5/min/IP**. Lockout: 5 failed logins → 15 minutes.""",
    [
        request_item(
            "Login",
            "POST",
            "/api/v1/provider/auth/login",
            f"""{md_table([
    ('email', 'Required', 'string', 'Valid email. Account must be UserType.Provider and IsActive=true.'),
    ('password', 'Required', 'string', 'Current password. 401 on mismatch. Locked after 5 failures.'),
])}""",
            body=json_pretty({"email": "{{providerEmail}}", "password": "{{providerPassword}}"}),
            tests=save_tokens("provider"),
            example_body=envelope(200, "Login successful.", AUTH_EXAMPLE.replace("Admin", "Provider").replace("admin@enterprise.local", "provider@enterprise.local")),
        ),
        request_item(
            "Forgot password",
            "POST",
            "/api/v1/provider/auth/forgot-password",
            f"""Always returns 200 (anti-enumeration). OTP emailed if account exists.

{md_table([
    ('email', 'Required', 'string', 'Valid email.'),
])}""",
            body=json_pretty({"email": "{{providerEmail}}"}),
            tests=ok_test(),
        ),
        request_item(
            "Reset password",
            "POST",
            "/api/v1/provider/auth/reset-password",
            f"""{md_table([
    ('email', 'Required', 'string', 'Valid email.'),
    ('otp', 'Required', 'string', 'Length 4–10.'),
    ('newPassword', 'Required', 'string', 'Min 8; 1 uppercase, 1 lowercase, 1 digit, 1 special. Example: `Provider@12345!`.'),
])}""",
            body=json_pretty({
                "email": "{{providerEmail}}",
                "otp": "{{providerResetOtp}}",
                "newPassword": "Provider@12345!",
            }),
            tests=ok_test(),
        ),
        request_item(
            "Change password",
            "POST",
            "/api/v1/provider/auth/change-password",
            f"""Authenticated provider.

{md_table([
    ('currentPassword', 'Required', 'string', 'Must match existing password. 401 if wrong.'),
    ('newPassword', 'Required', 'string', 'Strong password (min 8, upper, lower, digit, special).'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({"currentPassword": "{{providerPassword}}", "newPassword": "Provider@12345!"}),
            tests=ok_test(),
        ),
        request_item(
            "Refresh token",
            "POST",
            "/api/v1/provider/auth/refresh-token",
            f"""{md_table([
    ('refreshToken', 'Required', 'string', 'Must be a Provider-portal refresh token.'),
])}""",
            body=json_pretty({"refreshToken": "{{providerRefreshToken}}"}),
            tests=save_tokens("provider"),
        ),
        request_item(
            "Revoke token (logout)",
            "POST",
            "/api/v1/provider/auth/revoke-token",
            f"""{md_table([
    ('refreshToken', 'Required', 'string', 'Token to revoke.'),
])}""",
            body=json_pretty({"refreshToken": "{{providerRefreshToken}}"}),
            tests=ok_test(),
        ),
    ],
)

provider_profile = folder(
    "04. Provider Profile  /api/v1/provider/profile",
    f"""Requires `Bearer {{{{providerAccessToken}}}}` and `UserType.Provider`.

{IMAGE_RULES}""",
    [
        request_item(
            "Get profile",
            "GET",
            "/api/v1/provider/profile",
            "Account holder profile (name, email, userType). Store logo is on provider image endpoints / admin provider DTO.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Update profile",
            "PUT",
            "/api/v1/provider/profile",
            f"""{md_table([
    ('firstName', 'Required', 'string', 'Max 100.'),
    ('lastName', 'Required', 'string', 'Max 100.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({"firstName": "Demo", "lastName": "Provider"}),
            tests=ok_test(),
        ),
        request_item(
            "Upload store logo (file)",
            "POST",
            "/api/v1/provider/profile/image",
            f"""Uploads/replaces the store logo. Previous blob is deleted best-effort.

{IMAGE_RULES}

**Example returned imageUrl:**
`{{{{blobPublicBaseUrl}}}}/{{{{mediaContainer}}}}/providers/{{{{providerId}}}}/logo/7f3c2a1b9e8d4c6a5b0f1e2d3c4b5a69.jpg`""",
            auth_var="providerAccessToken",
            file_upload=True,
            tests=ok_test(),
            example_body=envelope(200, "Image uploaded successfully.",
                                  '{\n      "id": "{{providerId}}",\n      "imageUrl": "' + '{{blobPublicBaseUrl}}/{{mediaContainer}}/providers/{{providerId}}/logo/7f3c2a1b9e8d4c6a5b0f1e2d3c4b5a69.jpg"\n    }'),
        ),
        request_item(
            "Delete store logo",
            "DELETE",
            "/api/v1/provider/profile/image",
            "Removes the logo from Azure Blob and clears `imageUrl`.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Request email change",
            "POST",
            "/api/v1/provider/profile/change-email/request",
            f"""{md_table([
    ('newEmail', 'Required', 'string', 'Valid email, max 256. Unique. 409 if taken.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({"newEmail": "provider.new@example.com"}),
            tests=ok_test(),
        ),
        request_item(
            "Confirm email change",
            "POST",
            "/api/v1/provider/profile/change-email/confirm",
            f"""{md_table([
    ('newEmail', 'Required', 'string', 'Must match the pending request.'),
    ('otp', 'Required', 'string', 'Length 4–10.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({"newEmail": "provider.new@example.com", "otp": "{{providerResetOtp}}"}),
            tests=ok_test(),
        ),
    ],
)

admin_auth = folder(
    "05. Admin Auth  /api/v1/admin/auth",
    "Email + password. Rate limit **5/min/IP**. Lockout 5 failures / 15 minutes. Seeded Azure/dev: `admin@enterprise.local` / `Admin@12345!`.",
    [
        request_item(
            "Login",
            "POST",
            "/api/v1/admin/auth/login",
            f"""{md_table([
    ('email', 'Required', 'string', 'Valid email. Must be UserType.Admin and active.'),
    ('password', 'Required', 'string', 'Current password.'),
])}""",
            body=json_pretty({"email": "{{adminEmail}}", "password": "{{adminPassword}}"}),
            tests=save_tokens("admin"),
            example_body=envelope(200, "Login successful.", AUTH_EXAMPLE),
        ),
        request_item(
            "Forgot password",
            "POST",
            "/api/v1/admin/auth/forgot-password",
            f"""{md_table([
    ('email', 'Required', 'string', 'Valid email. Always 200 (anti-enumeration).'),
])}""",
            body=json_pretty({"email": "{{adminEmail}}"}),
            tests=ok_test(),
        ),
        request_item(
            "Reset password",
            "POST",
            "/api/v1/admin/auth/reset-password",
            f"""{md_table([
    ('email', 'Required', 'string', 'Valid email.'),
    ('otp', 'Required', 'string', 'Length 4–10.'),
    ('newPassword', 'Required', 'string', 'Strong password: min 8, upper, lower, digit, special.'),
])}""",
            body=json_pretty({
                "email": "{{adminEmail}}",
                "otp": "{{adminResetOtp}}",
                "newPassword": "Admin@12345!",
            }),
            tests=ok_test(),
        ),
        request_item(
            "Change password",
            "POST",
            "/api/v1/admin/auth/change-password",
            f"""{md_table([
    ('currentPassword', 'Required', 'string', 'Must match existing. 401 if wrong.'),
    ('newPassword', 'Required', 'string', 'Strong password rules.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({"currentPassword": "{{adminPassword}}", "newPassword": "Admin@12345!"}),
            tests=ok_test(),
        ),
        request_item(
            "Refresh token",
            "POST",
            "/api/v1/admin/auth/refresh-token",
            f"""{md_table([
    ('refreshToken', 'Required', 'string', 'Must be an Admin-portal refresh token.'),
])}""",
            body=json_pretty({"refreshToken": "{{adminRefreshToken}}"}),
            tests=save_tokens("admin"),
        ),
        request_item(
            "Revoke token (logout)",
            "POST",
            "/api/v1/admin/auth/revoke-token",
            f"""{md_table([
    ('refreshToken', 'Required', 'string', 'Token to revoke.'),
])}""",
            body=json_pretty({"refreshToken": "{{adminRefreshToken}}"}),
            tests=ok_test(),
        ),
    ],
)

admin_profile = folder(
    "06. Admin Profile  /api/v1/admin/profile",
    "Requires `Bearer {{adminAccessToken}}` and `UserType.Admin`.",
    [
        request_item("Get profile", "GET", "/api/v1/admin/profile", "Current administrator profile.", auth_var="adminAccessToken", tests=ok_test(), example_body=envelope(200, "Profile retrieved successfully.", PROFILE_EXAMPLE)),
        request_item(
            "Update profile",
            "PUT",
            "/api/v1/admin/profile",
            f"""{md_table([
    ('firstName', 'Required', 'string', 'Max 100.'),
    ('lastName', 'Required', 'string', 'Max 100.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({"firstName": "System", "lastName": "Administrator"}),
            tests=ok_test(),
        ),
        request_item(
            "Request email change",
            "POST",
            "/api/v1/admin/profile/change-email/request",
            f"""{md_table([
    ('newEmail', 'Required', 'string', 'Valid email, max 256. Unique.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({"newEmail": "admin.new@enterprise.local"}),
            tests=ok_test(),
        ),
        request_item(
            "Confirm email change",
            "POST",
            "/api/v1/admin/profile/change-email/confirm",
            f"""{md_table([
    ('newEmail', 'Required', 'string', 'Must match pending request.'),
    ('otp', 'Required', 'string', 'Length 4–10.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({"newEmail": "admin.new@enterprise.local", "otp": "{{adminResetOtp}}"}),
            tests=ok_test(),
        ),
    ],
)

admin_notifications = folder(
    "06b. Admin Notifications  /api/v1/admin/notifications",
    """In-app + FCM notifications for the signed-in admin.

- `GET /notifications` returns the inbox (newest first) and **marks all unread as read**.
- `GET /notifications/count` returns `{ unreadCount }` without marking read.
- Register an FCM device token after login; unregister on logout.
- Deep link fields: `notificationType` + `notificationId` (related entity Guid).

Currently the only wired event is **Create Provider** → other admins get `new_provider_registration`.""",
    [
        request_item(
            "List notifications (marks all read)",
            "GET",
            "/api/v1/admin/notifications",
            "Returns the current admin's notifications newest-first, then marks all unread as read.",
            auth_var="adminAccessToken",
            tests=ok_test(),
            example_body=envelope(
                200,
                "Notifications retrieved successfully.",
                """[
      {
        "id": "11111111-1111-1111-1111-111111111111",
        "title": "New provider registered",
        "body": "\\"Demo Restaurant\\" was added to the marketplace.",
        "notificationType": "new_provider_registration",
        "notificationId": "22222222-2222-2222-2222-222222222222",
        "isRead": true,
        "createdAtUtc": "2026-09-25T12:00:00Z"
      }
    ]""",
            ),
        ),
        request_item(
            "Unread notification count",
            "GET",
            "/api/v1/admin/notifications/count",
            "Badge count only — does not mark notifications as read.",
            auth_var="adminAccessToken",
            tests=ok_test(),
            example_body=envelope(
                200,
                "Unread notification count retrieved successfully.",
                '{ "unreadCount": 3 }',
            ),
        ),
        request_item(
            "Mark notification read",
            "POST",
            "/api/v1/admin/notifications/{{notificationId}}/read",
            "Marks one inbox row as read. 404 when missing or owned by another user.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Register device token",
            "POST",
            "/api/v1/admin/device-tokens",
            f"""Upsert FCM registration token for the current admin.

{md_table([
    ('token', 'Required', 'string', 'FCM device token from the client SDK. Max 512. Unique globally — moves to this user if previously owned by another.'),
    ('platform', 'Optional', 'string', 'android | ios | web. Max 32.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({"token": "fcm-device-token-example", "platform": "web"}),
            tests=ok_test(),
        ),
        request_item(
            "Unregister device token",
            "DELETE",
            "/api/v1/admin/device-tokens",
            f"""Remove an FCM token (call on logout).

{md_table([
    ('token', 'Required', 'string', 'Same FCM token previously registered.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({"token": "fcm-device-token-example"}),
            tests=ok_test(),
        ),
    ],
)

admin_providers = folder(
    "07. Admin Providers  /api/v1/admin/providers",
    f"""Admin-only merchant onboarding. Permissions: `Providers.Read|Create|Update|Delete`.

{IMAGE_RULES}""",
    [
        request_item(
            "List providers",
            "GET",
            "/api/v1/admin/providers",
            "Paged merchant list. Requires `Providers.Read`.",
            auth_var="adminAccessToken",
            query=PAGING + [
                q("searchTerm", "", "OPTIONAL. Filters company name / contact / email."),
                q("isActive", "true", "OPTIONAL. `true` | `false`. Omit for all.", disabled=True),
            ],
            tests=ok_test(),
        ),
        request_item(
            "Get provider by id",
            "GET",
            "/api/v1/admin/providers/{{providerId}}",
            "Full provider + identity join. `imageUrl` is an Azure Blob public URL when a logo exists.",
            auth_var="adminAccessToken",
            tests=ok_test(),
            example_body=envelope(200, "Provider retrieved successfully.", """{
      "id": "{{providerId}}",
      "userId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      "email": "seller@example.com",
      "firstName": "Sara",
      "lastName": "Khan",
      "companyName": "Sara Kitchen",
      "phoneNumber": "+201000000009",
      "serviceId": "{{serviceId}}",
      "serviceName": "Restaurant",
      "isActive": true,
      "emailConfirmed": true,
      "imageUrl": "{{blobPublicBaseUrl}}/{{mediaContainer}}/providers/{{providerId}}/logo/7f3c2a1b9e8d4c6a5b0f1e2d3c4b5a69.jpg",
      "createdAtUtc": "2026-09-19T10:00:00Z",
      "lastModifiedAtUtc": null
    }"""),
        ),
        request_item(
            "Create provider",
            "POST",
            "/api/v1/admin/providers",
            f"""Provisions Identity user (`UserType.Provider`) + store profile. Sends welcome email. Requires `Providers.Create`.

Logo is **not** in this body — upload it afterwards (optional).

{md_table([
    ('email', 'Required', 'string', 'Valid email, max 256, unique. 409 if taken.'),
    ('password', 'Required', 'string', 'Strong: min 8, upper, lower, digit, special.'),
    ('firstName', 'Required', 'string', 'Max 100.'),
    ('lastName', 'Required', 'string', 'Max 100.'),
    ('companyName', 'Required', 'string', 'Max 200.'),
    ('phoneNumber', 'Optional', 'string | null', 'Max 40. Include even if null.'),
    ('serviceId', 'Optional', 'guid | null', 'Marketplace service id from GET /admin/services/lookup. Must exist if provided.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({
                "email": "{{providerEmail}}",
                "password": "{{providerPassword}}",
                "firstName": "Sara",
                "lastName": "Khan",
                "companyName": "Sara Kitchen",
                "phoneNumber": "+201000000009",
                "serviceId": "{{serviceId}}",
            }),
            tests=save_id("providerId") + [
                "if (res.success && res.data && res.data.userId) pm.collectionVariables.set('providerUserId', res.data.userId);",
            ],
            example_status=201,
        ),
        request_item(
            "Update provider",
            "PUT",
            "/api/v1/admin/providers/{{providerId}}",
            f"""Requires `Providers.Update`. Email/password are not changed here.

{md_table([
    ('firstName', 'Required', 'string', 'Max 100.'),
    ('lastName', 'Required', 'string', 'Max 100.'),
    ('companyName', 'Required', 'string', 'Max 200.'),
    ('phoneNumber', 'Optional', 'string | null', 'Max 40.'),
    ('serviceId', 'Optional', 'guid | null', 'Reassigns marketplace service. Send `null` to clear.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({
                "firstName": "Sara",
                "lastName": "Khan",
                "companyName": "Sara Kitchen Downtown",
                "phoneNumber": "+201000000009",
                "serviceId": "{{serviceId}}",
            }),
            tests=ok_test(),
        ),
        request_item(
            "Set provider active",
            "POST",
            "/api/v1/admin/providers/{{providerId}}/set-active",
            f"""Requires `Providers.Update`. Inactive providers cannot log in.

{md_table([
    ('isActive', 'Required', 'boolean', '`true` activate / `false` deactivate.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({"isActive": True}),
            tests=ok_test(),
        ),
        request_item(
            "Delete provider",
            "DELETE",
            "/api/v1/admin/providers/{{providerId}}",
            "Soft-deletes store and disables the user. Requires `Providers.Delete`. System accounts are protected.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Upload provider logo (file)",
            "POST",
            "/api/v1/admin/providers/{{providerId}}/image",
            f"""Requires `Providers.Update`. Logo is optional on create — call this after create.

{IMAGE_RULES}""",
            auth_var="adminAccessToken",
            file_upload=True,
            tests=ok_test(),
        ),
        request_item(
            "Delete provider logo",
            "DELETE",
            "/api/v1/admin/providers/{{providerId}}/image",
            "Requires `Providers.Update`. Clears Azure blob + `imageUrl`.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
    ],
)

admin_services = folder(
    "08. Admin Marketplace Services  /api/v1/admin/services",
    f"""Business categories (restaurant, pharmacy, …). Permissions: `Services.*`.

Localized text JSON: `{{ "en": "...", "it": "...", "ar": "..." }}`. English required; `it`/`ar` optional.

{IMAGE_RULES}""",
    [
        request_item(
            "List services",
            "GET",
            "/api/v1/admin/services",
            "Paged list. Requires `Services.Read`. `name`/`description` follow `Accept-Language`.",
            auth_var="adminAccessToken",
            query=PAGING + [
                q("searchTerm", "", "OPTIONAL. Filters code or localized name."),
                q("isActive", "true", "OPTIONAL. `true` | `false`.", disabled=True),
            ],
            tests=ok_test(),
        ),
        request_item(
            "Services lookup",
            "GET",
            "/api/v1/admin/services/lookup",
            "Active services for dropdowns (`id`, `code`, `name`, `description`). Requires `Services.Read`. Save `serviceId` from a result if needed.",
            auth_var="adminAccessToken",
            tests=[
                "const res = pm.response.json();",
                "if (res.success && Array.isArray(res.data) && res.data.length) {",
                "    pm.collectionVariables.set('serviceId', res.data[0].id);",
                "}",
                "pm.test('Lookup ok', function () { pm.expect(res.success).to.eql(true); });",
            ],
        ),
        request_item(
            "Get service by id",
            "GET",
            "/api/v1/admin/services/{{serviceId}}",
            "Full translations + optional Azure `imageUrl`. Requires `Services.Read`.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Create service",
            "POST",
            "/api/v1/admin/services",
            f"""Requires `Services.Create`. Image is **optional** and uploaded separately.

{md_table([
    ('code', 'Required', 'string', 'Max 100. Pattern `^[A-Za-z0-9-_]+$`. Unique among non-deleted services. 409 if duplicate.'),
    ('name', 'Required', 'LocalizedText', 'Object with `en` (required, max 200), `it` optional max 200, `ar` optional max 200.'),
    ('name.en', 'Required', 'string', 'English display name, max 200.'),
    ('name.it', 'Optional', 'string | null', 'Italian, max 200. Falls back to English.'),
    ('name.ar', 'Optional', 'string | null', 'Arabic, max 200. Falls back to English.'),
    ('description', 'Optional', 'LocalizedText | null', 'Same shape as name. Each language max 2000. `en` not required when object is present.'),
    ('description.en', 'Optional', 'string', 'Max 2000.'),
    ('description.it', 'Optional', 'string | null', 'Max 2000.'),
    ('description.ar', 'Optional', 'string | null', 'Max 2000.'),
    ('displayOrder', 'Optional', 'int', 'Default 0. Must be >= 0.'),
    ('isActive', 'Optional', 'boolean', 'Default `true`.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({
                "code": "electronics",
                "displayOrder": 4,
                "isActive": True,
                "name": {
                    "en": "Consumer Electronics",
                    "it": "Elettronica di consumo",
                    "ar": "الأجهزة الإلكترونية",
                },
                "description": {
                    "en": "Smartphones, laptops, and smart gadgets",
                    "it": "Smartphone, laptop e gadget intelligenti",
                    "ar": "الهواتف الذكية وأجهزة الحاسوب والأجهزة الذكية",
                },
            }),
            tests=save_id("serviceId"),
            example_status=201,
        ),
        request_item(
            "Update service",
            "PUT",
            "/api/v1/admin/services/{{serviceId}}",
            f"""Requires `Services.Update`. `isActive` is not in this body (use set-active).

{md_table([
    ('code', 'Required', 'string', 'Max 100, `^[A-Za-z0-9-_]+$`, unique.'),
    ('name', 'Required', 'LocalizedText', '`en` required max 200; `it`/`ar` optional max 200.'),
    ('description', 'Optional', 'LocalizedText | null', 'Each language max 2000.'),
    ('displayOrder', 'Optional', 'int', '>= 0. Default 0.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({
                "code": "electronics",
                "displayOrder": 4,
                "name": {"en": "Consumer Electronics", "it": "Elettronica di consumo", "ar": "الأجهزة الإلكترونية"},
                "description": {"en": "Phones and laptops", "it": "Telefoni e laptop", "ar": "هواتف وحواسيب"},
            }),
            tests=ok_test(),
        ),
        request_item(
            "Set service active",
            "POST",
            "/api/v1/admin/services/{{serviceId}}/set-active",
            f"""Requires `Services.Update`.

{md_table([
    ('isActive', 'Required', 'boolean', '`true` / `false`.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({"isActive": True}),
            tests=ok_test(),
        ),
        request_item(
            "Delete service",
            "DELETE",
            "/api/v1/admin/services/{{serviceId}}",
            "Requires `Services.Delete`. **409** if any active provider is linked (`Service.HasLinkedProviders`).",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Upload service image (file)",
            "POST",
            "/api/v1/admin/services/{{serviceId}}/image",
            f"""Requires `Services.Update`. Optional after create.

**Example imageUrl:** `{{{{blobPublicBaseUrl}}}}/{{{{mediaContainer}}}}/services/{{{{serviceId}}}}/9a8b7c6d5e4f3210.jpg`

{IMAGE_RULES}""",
            auth_var="adminAccessToken",
            file_upload=True,
            tests=ok_test(),
        ),
        request_item(
            "Delete service image",
            "DELETE",
            "/api/v1/admin/services/{{serviceId}}/image",
            "Requires `Services.Update`.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
    ],
)

admin_roles = folder(
    "09. Admin Roles  /api/v1/admin/roles",
    "Permissions: `Roles.Read|Create|Update|Delete`. System roles (`isSystem=true`, e.g. Admin) cannot be updated or deleted (409).",
    [
        request_item(
            "List admin roles",
            "GET",
            "/api/v1/admin/roles",
            "Requires `Roles.Read`.",
            auth_var="adminAccessToken",
            query=PAGING + [q("searchTerm", "", "OPTIONAL. Filters role name.")],
            tests=[
                "const res = pm.response.json();",
                "if (res.success && res.data && res.data.items && res.data.items.length) {",
                "    const custom = res.data.items.find(r => !r.isSystem) || res.data.items[0];",
                "    pm.collectionVariables.set('adminRoleId', custom.id);",
                "}",
                "pm.test('Roles listed', function () { pm.expect(res.success).to.eql(true); });",
            ],
        ),
        request_item(
            "Get admin role by id",
            "GET",
            "/api/v1/admin/roles/{{adminRoleId}}",
            "Requires `Roles.Read`.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Create admin role",
            "POST",
            "/api/v1/admin/roles",
            f"""Requires `Roles.Create`.

{md_table([
    ('name', 'Required', 'string', 'Max 100. Unique within Admin portal. 409 if duplicate.'),
    ('permissions', 'Required', 'string[]', 'Non-empty. Each value must be a valid **Admin** permission from GET /admin/permissions.'),
])}

Valid admin permissions: `{', '.join(ADMIN_PERMS)}`.""",
            auth_var="adminAccessToken",
            body=json_pretty({
                "name": "Catalog Supervisor",
                "permissions": ["Providers.Read", "Providers.Update", "Services.Read", "Services.Update"],
            }),
            tests=save_id("adminRoleId"),
            example_status=201,
        ),
        request_item(
            "Update admin role",
            "PUT",
            "/api/v1/admin/roles/{{adminRoleId}}",
            f"""Requires `Roles.Update`. System roles → 409.

{md_table([
    ('name', 'Required', 'string', 'Max 100, unique in Admin portal.'),
    ('permissions', 'Required', 'string[]', 'Replaces the full claim set. Must be valid Admin permissions.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({
                "name": "Catalog Supervisor",
                "permissions": ["Providers.Read", "Services.Read"],
            }),
            tests=ok_test(),
        ),
        request_item(
            "Delete admin role",
            "DELETE",
            "/api/v1/admin/roles/{{adminRoleId}}",
            "Requires `Roles.Delete`. System roles → 409 `Role.CannotDeleteSystemRole`.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
    ],
)

admin_permissions = folder(
    "10. Admin Permissions  /api/v1/admin/permissions",
    "Catalog for the admin role UI checkbox tree.",
    [
        request_item(
            "Get admin permissions catalog",
            "GET",
            "/api/v1/admin/permissions",
            "Requires `Roles.Read`. Grouped by module (`Admins`, `Roles`, `Providers`, `Services`, `ApiKeys`).",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
    ],
)

admin_users = folder(
    "11. Admin Staff  /api/v1/admin/users",
    "Platform administrators (not merchants). Permissions: `Admins.*`. System admin cannot be updated/deactivated/deleted (409).",
    [
        request_item(
            "List admin staff",
            "GET",
            "/api/v1/admin/users",
            "Requires `Admins.Read`.",
            auth_var="adminAccessToken",
            query=PAGING + [
                q("searchTerm", "", "OPTIONAL. Email, name, phone."),
                q("roleId", "{{adminRoleId}}", "OPTIONAL. Filter by assigned admin role GUID.", disabled=True),
                q("isActive", "true", "OPTIONAL. `true` | `false`.", disabled=True),
            ],
            tests=ok_test(),
        ),
        request_item(
            "Get admin staff by id",
            "GET",
            "/api/v1/admin/users/{{adminStaffId}}",
            "Includes `permissions` list. Requires `Admins.Read`.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Create admin staff",
            "POST",
            "/api/v1/admin/users",
            f"""Requires `Admins.Create`. Welcome email is sent; rollback if email fails.

{md_table([
    ('firstName', 'Required', 'string', 'Max 50.'),
    ('lastName', 'Required', 'string', 'Max 50.'),
    ('email', 'Required', 'string', 'Valid email, max 256, unique. 409 if taken.'),
    ('phoneNumber', 'Optional', 'string | null', 'Included even when unused. No strict format validator.'),
    ('password', 'Required', 'string', 'Strong: min 8, upper, lower, digit, special.'),
    ('roleId', 'Required', 'guid', 'Must be an Admin-portal role (not a store role).'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({
                "firstName": "Sarah",
                "lastName": "Connor",
                "email": "sarah.connor@enterprise.local",
                "phoneNumber": "+1234567890",
                "password": "AdminStaff@12345!",
                "roleId": "{{adminRoleId}}",
            }),
            tests=save_id("adminStaffId"),
            example_status=201,
        ),
        request_item(
            "Update admin staff",
            "PUT",
            "/api/v1/admin/users/{{adminStaffId}}",
            f"""Requires `Admins.Update`. Cannot modify system admin.

{md_table([
    ('firstName', 'Required', 'string', 'Max 50.'),
    ('lastName', 'Required', 'string', 'Max 50.'),
    ('phoneNumber', 'Optional', 'string | null', 'Send null to clear.'),
    ('roleId', 'Required', 'guid', 'Admin-portal role id.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({
                "firstName": "Sarah",
                "lastName": "Connor",
                "phoneNumber": "+1234567899",
                "roleId": "{{adminRoleId}}",
            }),
            tests=ok_test(),
        ),
        request_item(
            "Set admin staff active",
            "POST",
            "/api/v1/admin/users/{{adminStaffId}}/set-active",
            f"""Requires `Admins.Update`. Cannot deactivate system admin (409).

{md_table([
    ('isActive', 'Required', 'boolean', '`true` / `false`.'),
])}""",
            auth_var="adminAccessToken",
            body=json_pretty({"isActive": True}),
            tests=ok_test(),
        ),
        request_item(
            "Delete admin staff",
            "DELETE",
            "/api/v1/admin/users/{{adminStaffId}}",
            "Requires `Admins.Delete`. System admin → 409 `Role.CannotDeleteSystemUser`.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
    ],
)

provider_roles = folder(
    "12. Provider Roles  /api/v1/provider/roles",
    "Store-scoped staff roles. Tenant = caller `ProviderId`. Super Provider system role cannot be updated/deleted. Permissions: `ProviderRoles.*`.",
    [
        request_item(
            "List store roles",
            "GET",
            "/api/v1/provider/roles",
            "Requires `ProviderRoles.Read`. Auto-scoped to the logged-in store.",
            auth_var="providerAccessToken",
            query=PAGING + [q("searchTerm", "", "OPTIONAL. Role name.")],
            tests=[
                "const res = pm.response.json();",
                "if (res.success && res.data && res.data.items && res.data.items.length) {",
                "    const custom = res.data.items.find(r => !r.isSystem) || res.data.items[0];",
                "    pm.collectionVariables.set('providerRoleId', custom.id);",
                "}",
                "pm.test('Roles listed', function () { pm.expect(res.success).to.eql(true); });",
            ],
        ),
        request_item(
            "Get store role by id",
            "GET",
            "/api/v1/provider/roles/{{providerRoleId}}",
            "Requires `ProviderRoles.Read`. 404 if the role belongs to another store.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Create store role",
            "POST",
            "/api/v1/provider/roles",
            f"""Requires `ProviderRoles.Create`. Bound to caller store.

{md_table([
    ('name', 'Required', 'string', 'Max 100. Unique within this store. 409 if duplicate.'),
    ('permissions', 'Required', 'string[]', 'Non-empty. Must be valid **Provider** permissions from GET /provider/permissions.'),
])}

Valid provider permissions: `{', '.join(PROVIDER_PERMS)}`.""",
            auth_var="providerAccessToken",
            body=json_pretty({
                "name": "Head Cashier",
                "permissions": [
                    "ProviderRoles.Read",
                    "ProviderStaff.Read",
                    "ProviderCategory.Read",
                    "ProviderProduct.Read",
                    "ProviderProduct.Update",
                ],
            }),
            tests=save_id("providerRoleId"),
            example_status=201,
        ),
        request_item(
            "Update store role",
            "PUT",
            "/api/v1/provider/roles/{{providerRoleId}}",
            f"""Requires `ProviderRoles.Update`. System Super Provider → 409.

{md_table([
    ('name', 'Required', 'string', 'Max 100, unique in this store.'),
    ('permissions', 'Required', 'string[]', 'Full replace. Provider-portal permissions only.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({
                "name": "Head Cashier",
                "permissions": ["ProviderStaff.Read", "ProviderProduct.Read"],
            }),
            tests=ok_test(),
        ),
        request_item(
            "Delete store role",
            "DELETE",
            "/api/v1/provider/roles/{{providerRoleId}}",
            "Requires `ProviderRoles.Delete`. System role → 409.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
    ],
)

provider_permissions = folder(
    "13. Provider Permissions  /api/v1/provider/permissions",
    "Catalog for store role UI.",
    [
        request_item(
            "Get provider permissions catalog",
            "GET",
            "/api/v1/provider/permissions",
            "Requires `ProviderRoles.Read`. Modules: ProviderRoles, ProviderStaff, ProviderCategory, ProviderProduct.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
    ],
)

provider_staff = folder(
    "14. Provider Staff  /api/v1/provider/staff",
    "Store employees. Tenant-scoped. Store owner (`isSystem`) cannot be updated/deactivated/deleted. Permissions: `ProviderStaff.*`.",
    [
        request_item(
            "List store staff",
            "GET",
            "/api/v1/provider/staff",
            "Requires `ProviderStaff.Read`.",
            auth_var="providerAccessToken",
            query=PAGING + [
                q("searchTerm", "", "OPTIONAL. Email, name, phone."),
                q("roleId", "{{providerRoleId}}", "OPTIONAL. Filter by store role GUID.", disabled=True),
                q("isActive", "true", "OPTIONAL.", disabled=True),
            ],
            tests=ok_test(),
        ),
        request_item(
            "Get store staff by id",
            "GET",
            "/api/v1/provider/staff/{{providerStaffId}}",
            "Requires `ProviderStaff.Read`. 404 if other store.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Create store staff",
            "POST",
            "/api/v1/provider/staff",
            f"""Requires `ProviderStaff.Create`. Welcome email with dashboard link. Rollback if email fails.

{md_table([
    ('firstName', 'Required', 'string', 'Max 50.'),
    ('lastName', 'Required', 'string', 'Max 50.'),
    ('email', 'Required', 'string', 'Valid email, max 256, unique across the platform.'),
    ('phoneNumber', 'Optional', 'string | null', 'Include even when unused.'),
    ('password', 'Required', 'string', 'Strong password rules.'),
    ('roleId', 'Required', 'guid', 'Must belong to **this** store. 400/404 otherwise.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({
                "firstName": "Alex",
                "lastName": "Smith",
                "email": "alex.smith@example.com",
                "phoneNumber": "+1987654321",
                "password": "StoreStaff@12345!",
                "roleId": "{{providerRoleId}}",
            }),
            tests=save_id("providerStaffId"),
            example_status=201,
        ),
        request_item(
            "Update store staff",
            "PUT",
            "/api/v1/provider/staff/{{providerStaffId}}",
            f"""Requires `ProviderStaff.Update`. Cannot modify store owner.

{md_table([
    ('firstName', 'Required', 'string', 'Max 50.'),
    ('lastName', 'Required', 'string', 'Max 50.'),
    ('phoneNumber', 'Optional', 'string | null', 'Send null to clear.'),
    ('roleId', 'Required', 'guid', 'Must be a role of this store.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({
                "firstName": "Alex",
                "lastName": "Smith",
                "phoneNumber": "+1987654321",
                "roleId": "{{providerRoleId}}",
            }),
            tests=ok_test(),
        ),
        request_item(
            "Set store staff active",
            "POST",
            "/api/v1/provider/staff/{{providerStaffId}}/set-active",
            f"""Requires `ProviderStaff.Update`. Cannot deactivate store owner (409).

{md_table([
    ('isActive', 'Required', 'boolean', '`true` / `false`.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({"isActive": True}),
            tests=ok_test(),
        ),
        request_item(
            "Delete store staff",
            "DELETE",
            "/api/v1/provider/staff/{{providerStaffId}}",
            "Requires `ProviderStaff.Delete`. Store owner → 409.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
    ],
)

provider_services = folder(
    "15. Provider Services Lookup  /api/v1/provider/services",
    "Active marketplace services for the merchant dashboard (localized).",
    [
        request_item(
            "Provider services lookup",
            "GET",
            "/api/v1/provider/services/lookup",
            "Requires provider JWT (`[RequireProvider]`). No extra permission. Same shape as admin lookup.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
    ],
)

provider_categories = folder(
    "16. Provider Categories  /api/v1/provider/categories",
    f"""Store catalog categories. Tenant-scoped. Permissions: `ProviderCategory.*`.

{IMAGE_RULES}""",
    [
        request_item(
            "Create category",
            "POST",
            "/api/v1/provider/categories",
            f"""Requires `ProviderCategory.Create`. Image is **optional** — upload after create.

{md_table([
    ('name', 'Required', 'LocalizedText', '`en` required max 200; `it`/`ar` optional max 200.'),
    ('name.en', 'Required', 'string', 'English name, max 200.'),
    ('name.it', 'Optional', 'string | null', 'Italian, max 200.'),
    ('name.ar', 'Optional', 'string | null', 'Arabic, max 200.'),
    ('description', 'Optional', 'LocalizedText | null', 'Each language max 2000.'),
    ('description.en', 'Optional', 'string', 'Max 2000.'),
    ('description.it', 'Optional', 'string | null', 'Max 2000.'),
    ('description.ar', 'Optional', 'string | null', 'Max 2000.'),
    ('displayOrder', 'Optional', 'int', 'Default 0. Must be >= 0.'),
    ('isActive', 'Optional', 'boolean', 'Default `true`.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({
                "name": {"en": "Pizza", "it": "Pizza", "ar": "بيتزا"},
                "description": {"en": "Wood-fired pizzas", "it": "Pizze al forno a legna", "ar": "بيتزا فرن حطب"},
                "displayOrder": 1,
                "isActive": True,
            }),
            tests=save_id("categoryId"),
            example_status=201,
        ),
        request_item(
            "List categories",
            "GET",
            "/api/v1/provider/categories",
            "Requires `ProviderCategory.Read`. Returns all categories for this store (not paged).",
            auth_var="providerAccessToken",
            query=[q("isActive", "true", "OPTIONAL. `true` | `false`. Omit for all.", disabled=True)],
            tests=[
                "const res = pm.response.json();",
                "if (res.success && Array.isArray(res.data) && res.data.length) {",
                "    pm.collectionVariables.set('categoryId', res.data[0].id);",
                "}",
                "pm.test('Categories listed', function () { pm.expect(res.success).to.eql(true); });",
            ],
        ),
        request_item(
            "Categories lookup",
            "GET",
            "/api/v1/provider/categories/lookup",
            "Lightweight `{ id, name, displayOrder }` for product forms. Requires `ProviderCategory.Read`.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Get category by id",
            "GET",
            "/api/v1/provider/categories/{{categoryId}}",
            "Includes translations and Azure `imageUrl`. Requires `ProviderCategory.Read`.",
            auth_var="providerAccessToken",
            tests=ok_test(),
            example_body=envelope(200, "Category retrieved successfully.", """{
      "id": "{{categoryId}}",
      "providerId": "{{providerId}}",
      "name": "Pizza",
      "description": "Wood-fired pizzas",
      "displayOrder": 1,
      "isActive": true,
      "imageUrl": "{{blobPublicBaseUrl}}/{{mediaContainer}}/providers/{{providerId}}/categories/{{categoryId}}/aa11bb22cc33dd44ee55ff6677889900.jpg",
      "translations": {
        "name": { "en": "Pizza", "it": "Pizza", "ar": "بيتزا" },
        "description": { "en": "Wood-fired pizzas", "it": "Pizze al forno a legna", "ar": "بيتزا فرن حطب" }
      }
    }"""),
        ),
        request_item(
            "Update category",
            "PUT",
            "/api/v1/provider/categories/{{categoryId}}",
            f"""Requires `ProviderCategory.Update`. `isActive` is not here (use set-active).

{md_table([
    ('name', 'Required', 'LocalizedText', '`en` required max 200; `it`/`ar` optional.'),
    ('description', 'Optional', 'LocalizedText | null', 'Each language max 2000.'),
    ('displayOrder', 'Optional', 'int', '>= 0. Default 0.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({
                "name": {"en": "Pizza", "it": "Pizza", "ar": "بيتزا"},
                "description": {"en": "All pizzas", "it": "Tutte le pizze", "ar": "كل البيتزا"},
                "displayOrder": 1,
            }),
            tests=ok_test(),
        ),
        request_item(
            "Set category active",
            "POST",
            "/api/v1/provider/categories/{{categoryId}}/set-active",
            f"""Requires `ProviderCategory.Update`.

{md_table([
    ('isActive', 'Required', 'boolean', '`true` / `false`.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({"isActive": True}),
            tests=ok_test(),
        ),
        request_item(
            "Delete category",
            "DELETE",
            "/api/v1/provider/categories/{{categoryId}}",
            f"""Requires `ProviderCategory.Delete`.

{md_table([
    ('deleteRelatedProducts', 'Optional query', 'boolean', 'Default `false`. If the category has products and this is false → **409**. If `true`, related products are soft-deleted with the category.'),
])}""",
            auth_var="providerAccessToken",
            query=[q("deleteRelatedProducts", "false", "OPTIONAL. Default false. Set true to also soft-delete products in this category.")],
            tests=ok_test(),
        ),
        request_item(
            "Upload category image (file)",
            "POST",
            "/api/v1/provider/categories/{{categoryId}}/image",
            f"""Requires `ProviderCategory.Update`. Optional after create.

**Example imageUrl:** `{{{{blobPublicBaseUrl}}}}/{{{{mediaContainer}}}}/providers/{{{{providerId}}}}/categories/{{{{categoryId}}}}/aa11bb22cc33dd44ee55ff6677889900.jpg`

{IMAGE_RULES}""",
            auth_var="providerAccessToken",
            file_upload=True,
            tests=ok_test(),
        ),
        request_item(
            "Delete category image",
            "DELETE",
            "/api/v1/provider/categories/{{categoryId}}/image",
            "Requires `ProviderCategory.Update`.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
    ],
)

provider_products = folder(
    "17. Provider Products  /api/v1/provider/categories/{{categoryId}}/products",
    f"""Products nested under a category. Permissions: `ProviderProduct.*`.

**ProductStatus enum (JSON number):** `0` Draft, `1` Active, `2` Inactive. Query string also accepts `Draft` / `Active` / `Inactive`.

SKU unique per provider. Image is **optional** on create — upload after.

{IMAGE_RULES}""",
    [
        request_item(
            "List products in category",
            "GET",
            "/api/v1/provider/categories/{{categoryId}}/products",
            "Requires `ProviderProduct.Read`. 404 if category is not in this store.",
            auth_var="providerAccessToken",
            query=[q("status", "Active", "OPTIONAL. `Draft` | `Active` | `Inactive` or `0` | `1` | `2`. Omit for all statuses.", disabled=True)],
            tests=[
                "const res = pm.response.json();",
                "if (res.success && Array.isArray(res.data) && res.data.length) {",
                "    pm.collectionVariables.set('productId', res.data[0].id);",
                "}",
                "pm.test('Products listed', function () { pm.expect(res.success).to.eql(true); });",
            ],
        ),
        request_item(
            "Get product by id",
            "GET",
            "/api/v1/provider/categories/{{categoryId}}/products/{{productId}}",
            "Requires `ProviderProduct.Read`. `imageUrl` is Azure Blob when set.",
            auth_var="providerAccessToken",
            tests=ok_test(),
            example_body=envelope(200, "Product retrieved successfully.", """{
      "id": "{{productId}}",
      "categoryId": "{{categoryId}}",
      "name": "Margherita",
      "description": "Tomato, mozzarella, basil",
      "translations": {
        "name": { "en": "Margherita", "it": "Margherita", "ar": "مارغريتا" },
        "description": { "en": "Tomato, mozzarella, basil", "it": "Pomodoro, mozzarella, basilico", "ar": "طماطم وموزاريلا وريحان" }
      },
      "sku": "PIZ-MARG-001",
      "price": 9.50,
      "status": 1,
      "imageUrl": "{{blobPublicBaseUrl}}/{{mediaContainer}}/providers/{{providerId}}/products/{{productId}}/cc11dd22ee33ff44556677889900aabb.jpg"
    }"""),
        ),
        request_item(
            "Create product",
            "POST",
            "/api/v1/provider/categories/{{categoryId}}/products",
            f"""Requires `ProviderProduct.Create`. 409 if SKU already used in this store.

{md_table([
    ('name', 'Required', 'LocalizedText', '`en` required max 200; `it`/`ar` optional max 200.'),
    ('name.en', 'Required', 'string', 'English product name.'),
    ('name.it', 'Optional', 'string | null', 'Italian, max 200.'),
    ('name.ar', 'Optional', 'string | null', 'Arabic, max 200.'),
    ('description', 'Optional', 'LocalizedText | null', 'Each language max 2000.'),
    ('description.en', 'Optional', 'string', 'Max 2000.'),
    ('description.it', 'Optional', 'string | null', 'Max 2000.'),
    ('description.ar', 'Optional', 'string | null', 'Max 2000.'),
    ('sku', 'Required', 'string', 'Max 50. Pattern `^[A-Za-z0-9-_]+$`. Unique per provider.'),
    ('price', 'Required', 'decimal', 'Must be **greater than 0** (not 0).'),
    ('status', 'Required', 'int (enum)', '`0` Draft, `1` Active, `2` Inactive. Default on DTO is Draft if omitted by client serializer, but send it explicitly.'),
])}

No `file` / `imageUrl` in this JSON. Use **Upload product image** next (optional).""",
            auth_var="providerAccessToken",
            body=json_pretty({
                "name": {"en": "Margherita", "it": "Margherita", "ar": "مارغريتا"},
                "description": {
                    "en": "Tomato, mozzarella, basil",
                    "it": "Pomodoro, mozzarella, basilico",
                    "ar": "طماطم وموزاريلا وريحان",
                },
                "sku": "PIZ-MARG-001",
                "price": 9.50,
                "status": 1,
            }),
            tests=save_id("productId"),
            example_status=201,
        ),
        request_item(
            "Update product",
            "PUT",
            "/api/v1/provider/categories/{{categoryId}}/products/{{productId}}",
            f"""Requires `ProviderProduct.Update`. 409 on SKU clash.

{md_table([
    ('name', 'Required', 'LocalizedText', '`en` required max 200; `it`/`ar` optional.'),
    ('description', 'Optional', 'LocalizedText | null', 'Each language max 2000.'),
    ('sku', 'Required', 'string', 'Max 50, `^[A-Za-z0-9-_]+$`, unique per provider.'),
    ('price', 'Required', 'decimal', '> 0.'),
    ('status', 'Required', 'int (enum)', '0 Draft / 1 Active / 2 Inactive.'),
])}""",
            auth_var="providerAccessToken",
            body=json_pretty({
                "name": {"en": "Margherita", "it": "Margherita", "ar": "مارغريتا"},
                "description": {
                    "en": "Classic margherita",
                    "it": "Margherita classica",
                    "ar": "مارغريتا كلاسيكية",
                },
                "sku": "PIZ-MARG-001",
                "price": 10.00,
                "status": 1,
            }),
            tests=ok_test(),
        ),
        request_item(
            "Delete product",
            "DELETE",
            "/api/v1/provider/categories/{{categoryId}}/products/{{productId}}",
            "Requires `ProviderProduct.Delete`. Soft-delete.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Upload product image (file)",
            "POST",
            "/api/v1/provider/categories/{{categoryId}}/products/{{productId}}/image",
            f"""Requires `ProviderProduct.Update`. Optional after create.

**Example imageUrl:** `{{{{blobPublicBaseUrl}}}}/{{{{mediaContainer}}}}/providers/{{{{providerId}}}}/products/{{{{productId}}}}/cc11dd22ee33ff44556677889900aabb.jpg`

{IMAGE_RULES}""",
            auth_var="providerAccessToken",
            file_upload=True,
            tests=ok_test(),
        ),
        request_item(
            "Delete product image",
            "DELETE",
            "/api/v1/provider/categories/{{categoryId}}/products/{{productId}}/image",
            "Requires `ProviderProduct.Update`.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
    ],
)


COLLECTION_DESC = f"""# Subito Marketplace API — complete Postman collection

**Default base URL (Azure App Service):**
`{AZURE_URL}`

Swagger: `{AZURE_URL}/swagger`

Switch to local with the **Subito Local** environment (`{LOCAL_URL}`).

---

## Unified envelope

Every JSON endpoint returns:

```json
{{
  "success": true,
  "statusCode": 200,
  "message": "Localized message",
  "errors": [],
  "data": {{}},
  "traceId": "00-..."
}}
```

Health checks (`/health/live`, `/health/ready`) return plain text instead.

## Headers

| Header | Required | Notes |
|---|---|---|
| `Accept-Language` | Recommended | `en` (default), `ar`, `it` |
| `Authorization` | Protected routes | `Bearer <accessToken>` — **portal-specific** (client / admin / provider) |
| `Content-Type` | JSON bodies | `application/json`. Multipart uploads: let Postman set the boundary. |

## Auth recap

| Portal | Login | Token vars |
|---|---|---|
| Client | Email OTP or Google/Facebook | `clientAccessToken` / `clientRefreshToken` |
| Admin | Email + password | `adminAccessToken` / `adminRefreshToken` |
| Provider | Email + password (created by Admin) | `providerAccessToken` / `providerRefreshToken` |

Access token ~15 min (prod) / 60 min (dev). Refresh token 7 days.

## Rate limits & lockout

- Auth routes: **5 requests / minute / IP** → 429
- Other routes: **100 / minute / IP** → 429
- Admin/Provider: 5 failed logins → 15 minute lockout

## Passwords (when used)

Min 8 characters, 1 uppercase, 1 lowercase, 1 digit, 1 non-alphanumeric. Example: `Admin@12345!`

## Images (optional on create)

Create/update JSON bodies have **no file field**. After the entity exists, call the matching `POST .../image` with form field **`file`**.

Allowed: JPEG, PNG, WebP. Max **2 MB**. `imageUrl` in responses is a public Azure Blob URL:

`{{{{blobPublicBaseUrl}}}}/{{{{mediaContainer}}}}/{{folder}}/{{guid}}.jpg`

Set `blobPublicBaseUrl` to your Azure Storage public endpoint (same value as `BlobStorage__PublicBaseUrl` on the App Service).

## Suggested order

1. Health → Admin Login  
2. Admin Services lookup/create → Admin Providers create → upload provider logo  
3. Provider Login → Categories → upload category image → Products → upload product image  
4. Client Register → Verify (OTP from email on Azure)
"""


client_browse = folder(
    "Client catalog browse",
    "Anonymous storefront reads. Active stores, categories, and products only.",
    [
        request_item(
            "List marketplace services",
            "GET",
            "/api/v1/client/services",
            "Paged active marketplace services (restaurant, pharmacy, etc.).",
            query=PAGING + [
                q("searchTerm", "", "OPTIONAL.", disabled=True),
                q("isActive", "true", "OPTIONAL.", disabled=True),
            ],
            tests=ok_test(),
        ),
        request_item(
            "Get marketplace service",
            "GET",
            "/api/v1/client/services/{{serviceId}}",
            "Service detail by id.",
            tests=ok_test(),
        ),
        request_item(
            "List providers for a service",
            "GET",
            "/api/v1/client/services/{{serviceId}}/providers",
            "Paged stores assigned to an active marketplace service. Optional `searchTerm` matches company name.",
            query=PAGING + [q("searchTerm", "", "OPTIONAL. Company name contains.", disabled=True)],
            tests=ok_test(),
        ),
        request_item(
            "Get provider",
            "GET",
            "/api/v1/client/providers/{{providerId}}",
            "Storefront header for an active, non-deleted store: company, phone, image, and service name.",
            tests=ok_test(),
        ),
        request_item(
            "List categories for a provider",
            "GET",
            "/api/v1/client/{{providerId}}/categories",
            "Active categories for an active store. Includes `displayOrder`.",
            query=PAGING + [q("searchTerm", "", "OPTIONAL.", disabled=True)],
            tests=ok_test(),
        ),
        request_item(
            "Get category",
            "GET",
            "/api/v1/client/{{providerId}}/categories/{{categoryId}}",
            "Active category detail for an active store.",
            tests=ok_test(),
        ),
        request_item(
            "List products in category",
            "GET",
            "/api/v1/client/categories/{{categoryId}}/products",
            "Active products in an active category whose store is public.",
            query=PAGING + [q("searchTerm", "", "OPTIONAL.", disabled=True)],
            tests=ok_test(),
        ),
        request_item(
            "Get product in category",
            "GET",
            "/api/v1/client/categories/{{categoryId}}/products/{{productId}}",
            "Active product scoped to the category. 404 when the product belongs to another category or the store is not public.",
            tests=ok_test(),
        ),
        request_item(
            "List products for a provider",
            "GET",
            "/api/v1/client/{{providerId}}/products",
            "Active products in active categories for one store. Optional `categoryId` limits the page to one category.",
            query=PAGING + [q("categoryId", "{{categoryId}}", "OPTIONAL. Category in this store.", disabled=True)],
            tests=ok_test(),
        ),
        request_item(
            "Get product",
            "GET",
            "/api/v1/client/products/{{productId}}",
            "Active product detail, including `categoryId` and `providerId` for add-to-cart.",
            tests=ok_test(),
        ),
    ],
)

client_cart = folder(
    "Client cart",
    "Per-provider shopping cart. Requires client JWT.",
    [
        request_item(
            "Get cart",
            "GET",
            "/api/v1/client/{{providerId}}/cart",
            "Current cart for this store, with live product prices.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Add cart item",
            "POST",
            "/api/v1/client/{{providerId}}/cart/items",
            "Adds or increments an active product that belongs to this store.",
            auth_var="clientAccessToken",
            body=json_pretty({"productId": "{{productId}}", "quantity": 1}),
            tests=ok_test(),
        ),
        request_item(
            "Update cart item quantity",
            "PUT",
            "/api/v1/client/{{providerId}}/cart/items/{{cartItemId}}",
            "Sets quantity for an existing cart line.",
            auth_var="clientAccessToken",
            body=json_pretty({"quantity": 2}),
            tests=ok_test(),
        ),
        request_item(
            "Remove cart item",
            "DELETE",
            "/api/v1/client/{{providerId}}/cart/items/{{cartItemId}}",
            "Removes one line from the cart.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Clear cart",
            "DELETE",
            "/api/v1/client/{{providerId}}/cart",
            "Deletes the entire cart for this store.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
    ],
)

client_payments = folder(
    "Client payments",
    "Creates a Stripe Checkout session from the cart. Order is created by the Stripe webhook after payment.",
    [
        request_item(
            "Create checkout session",
            "POST",
            "/api/v1/client/{{providerId}}/payments",
            "No body. Returns `sessionId` and Stripe Checkout `url`. Cart must contain at least one valid active product.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
    ],
)

client_orders = folder(
    "Client orders",
    "Authenticated client order history. Cancel is allowed only while status is Pending.",
    [
        request_item(
            "List all orders",
            "GET",
            "/api/v1/client/orders",
            "All orders for the signed-in client across providers.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "List orders by provider",
            "GET",
            "/api/v1/client/{{providerId}}/orders",
            "Orders for one store.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Get order",
            "GET",
            "/api/v1/client/orders/{{orderId}}",
            "Order detail with line snapshots.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Get order by Stripe session",
            "GET",
            "/api/v1/client/orders/by-session/{{sessionId}}",
            "Load the order created by the Stripe webhook for this checkout session. 404 if the session belongs to another client or the webhook has not created the order yet.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Cancel order",
            "POST",
            "/api/v1/client/orders/{{orderId}}/cancel",
            "Owner only. Succeeds when status is Pending. Later statuses return 409.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
    ],
)

client_notifications = folder(
    "Client notifications",
    "Client inbox + FCM device tokens. Listing marks all unread as read.",
    [
        request_item(
            "List notifications (marks all read)",
            "GET",
            "/api/v1/client/notifications",
            "Newest first, then marks all unread as read.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Unread notification count",
            "GET",
            "/api/v1/client/notifications/count",
            "Badge count only — does not mark as read.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Mark notification read",
            "POST",
            "/api/v1/client/notifications/{{notificationId}}/read",
            "404 when the notification is missing or belongs to another user.",
            auth_var="clientAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Register device token",
            "POST",
            "/api/v1/client/device-tokens",
            "Upsert FCM token for push.",
            auth_var="clientAccessToken",
            body=json_pretty({"token": "fcm-device-token-example", "platform": "android"}),
            tests=ok_test(),
        ),
        request_item(
            "Unregister device token",
            "DELETE",
            "/api/v1/client/device-tokens",
            "Remove FCM token on logout.",
            auth_var="clientAccessToken",
            body=json_pretty({"token": "fcm-device-token-example"}),
            tests=ok_test(),
        ),
    ],
)

provider_notifications = folder(
    "Provider notifications",
    "Merchant inbox + FCM device tokens. Listing marks all unread as read.",
    [
        request_item(
            "List notifications (marks all read)",
            "GET",
            "/api/v1/provider/notifications",
            "Newest first, then marks all unread as read.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Unread notification count",
            "GET",
            "/api/v1/provider/notifications/count",
            "Badge count only — does not mark as read.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Mark notification read",
            "POST",
            "/api/v1/provider/notifications/{{notificationId}}/read",
            "404 when the notification is missing or belongs to another user.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Register device token",
            "POST",
            "/api/v1/provider/device-tokens",
            "Upsert FCM token for push.",
            auth_var="providerAccessToken",
            body=json_pretty({"token": "fcm-device-token-example", "platform": "android"}),
            tests=ok_test(),
        ),
        request_item(
            "Unregister device token",
            "DELETE",
            "/api/v1/provider/device-tokens",
            "Remove FCM token on logout.",
            auth_var="providerAccessToken",
            body=json_pretty({"token": "fcm-device-token-example"}),
            tests=ok_test(),
        ),
    ],
)

stripe_webhook = folder(
    "Stripe webhook",
    "Called by Stripe, not by the mobile/web apps. Requires a valid Stripe-Signature header.",
    [
        {
            "name": "Stripe checkout.session.completed",
            "request": {
                "method": "POST",
                "header": [
                    {"key": "Content-Type", "value": "application/json"},
                    {
                        "key": "Stripe-Signature",
                        "value": "{{stripeSignature}}",
                        "description": "Stripe webhook signature (whsec_...). Use Stripe CLI `stripe listen --forward-to` in local/dev.",
                    },
                ],
                "url": url_obj("/api/v1/webhooks/stripe"),
                "description": (
                    "Raw Stripe event JSON body. Creates the Order from the cart after payment, "
                    "clears the cart, and notifies the provider. Returns plain 200 OK (not the API envelope)."
                ),
                "body": {
                    "mode": "raw",
                    "raw": json_pretty({
                        "id": "evt_test",
                        "type": "checkout.session.completed",
                        "data": {
                            "object": {
                                "id": "cs_test_session",
                                "metadata": {
                                    "cartId": "{{cartId}}",
                                    "userId": "{{clientUserId}}",
                                    "providerId": "{{providerId}}",
                                    "locale": "en",
                                },
                            }
                        },
                    }),
                    "options": {"raw": {"language": "json"}},
                },
            },
        },
    ],
)

provider_store = folder(
    "Provider store",
    "Merchant store profile. Service assignment stays admin-owned.",
    [
        request_item(
            "Get store",
            "GET",
            "/api/v1/provider/store",
            "Company name, phone, logo, and the assigned marketplace service (read-only).",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Update store",
            "PUT",
            "/api/v1/provider/store",
            "Updates company name and phone. Does not change `serviceId`.",
            auth_var="providerAccessToken",
            body='{\n  "companyName": "Demo Restaurant",\n  "phoneNumber": "+201000000000"\n}',
            tests=ok_test(),
        ),
    ],
)

provider_orders = folder(
    "Provider orders",
    "Kitchen queue. `from` and `to` are inclusive UTC instants on `orderDateUtc`. Requires ProviderOrder.Read / Update.",
    [
        request_item(
            "List orders",
            "GET",
            "/api/v1/provider/orders",
            "Paged store orders. Filter by status and order date.",
            auth_var="providerAccessToken",
            query=PAGING + [
                q("status", "Pending", "OPTIONAL. Pending | Accepted | Preparing | Ready | Completed | Cancelled.", disabled=True),
                q("from", "2026-01-01T00:00:00Z", "OPTIONAL. Inclusive UTC start.", disabled=True),
                q("to", "2026-12-31T23:59:59Z", "OPTIONAL. Inclusive UTC end.", disabled=True),
            ],
            tests=ok_test(),
        ),
        request_item(
            "Get order",
            "GET",
            "/api/v1/provider/orders/{{orderId}}",
            "Order detail with line snapshots for this store.",
            auth_var="providerAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Update order status",
            "PATCH",
            "/api/v1/provider/orders/{{orderId}}/status",
            "Allowed transitions: Pending→Accepted|Cancelled, Accepted→Preparing|Cancelled, Preparing→Ready|Cancelled, Ready→Completed|Cancelled.",
            auth_var="providerAccessToken",
            body=json_pretty({"status": "Accepted"}),
            tests=ok_test(),
        ),
    ],
)

provider_store_products = folder(
    "Provider store products",
    "Paged inventory across all categories for the signed-in store. Requires ProviderProduct.Read.",
    [
        request_item(
            "List store products",
            "GET",
            "/api/v1/provider/products",
            "Optional filters: categoryId, status, searchTerm.",
            auth_var="providerAccessToken",
            query=PAGING + [
                q("categoryId", "{{categoryId}}", "OPTIONAL.", disabled=True),
                q("status", "Active", "OPTIONAL. Draft | Active | Inactive.", disabled=True),
                q("searchTerm", "", "OPTIONAL. SKU or localized name.", disabled=True),
            ],
            tests=ok_test(),
        ),
    ],
)

admin_clients = folder(
    "Admin clients",
    "Requires Clients.Read / Clients.Update. Deactivate blocks client login.",
    [
        request_item(
            "List clients",
            "GET",
            "/api/v1/admin/clients",
            "Paged client accounts. Optional search on name, email, and phone.",
            auth_var="adminAccessToken",
            query=PAGING + [
                q("searchTerm", "", "OPTIONAL.", disabled=True),
                q("isActive", "true", "OPTIONAL.", disabled=True),
            ],
            tests=ok_test(),
        ),
        request_item(
            "Get client",
            "GET",
            "/api/v1/admin/clients/{{clientUserId}}",
            "Client account detail.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "Set client active",
            "POST",
            "/api/v1/admin/clients/{{clientUserId}}/set-active",
            "Activate or deactivate a client. Inactive clients cannot sign in.",
            auth_var="adminAccessToken",
            body='{\n  "isActive": false\n}',
            tests=ok_test(),
        ),
    ],
)

admin_orders = folder(
    "Admin orders",
    "Read-only platform oversight. Requires Orders.Read. Fulfillment stays on the provider status endpoint.",
    [
        request_item(
            "List orders",
            "GET",
            "/api/v1/admin/orders",
            "Cross-store paged orders. Filter by provider, status, and order date.",
            auth_var="adminAccessToken",
            query=PAGING + [
                q("providerId", "{{providerId}}", "OPTIONAL.", disabled=True),
                q("status", "Pending", "OPTIONAL.", disabled=True),
                q("from", "2026-01-01T00:00:00Z", "OPTIONAL. Inclusive UTC start.", disabled=True),
                q("to", "2026-12-31T23:59:59Z", "OPTIONAL. Inclusive UTC end.", disabled=True),
            ],
            tests=ok_test(),
        ),
        request_item(
            "Get order",
            "GET",
            "/api/v1/admin/orders/{{orderId}}",
            "Order detail with line snapshots.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
    ],
)

admin_provider_catalog = folder(
    "Admin provider catalog",
    "Read-only menu audit. Requires Providers.Read.",
    [
        request_item(
            "List provider categories",
            "GET",
            "/api/v1/admin/providers/{{providerId}}/categories",
            "All categories for one store, including inactive.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
        request_item(
            "List provider products",
            "GET",
            "/api/v1/admin/providers/{{providerId}}/products",
            "All products for one store, any status.",
            auth_var="adminAccessToken",
            tests=ok_test(),
        ),
    ],
)


def coll_var(key: str, value: str, desc: str) -> dict:
    return {"key": key, "value": value, "type": "string", "description": desc}


variables = [
    coll_var("baseUrl", AZURE_URL, "API host. Azure App Service by default. Override with an environment."),
    coll_var("blobPublicBaseUrl", BLOB_PLACEHOLDER, "Azure Blob public base (BlobStorage__PublicBaseUrl). Used in example imageUrl values."),
    coll_var("mediaContainer", "media", "Blob container name (BlobStorage__ContainerName)."),
    coll_var("lang", "en", "Accept-Language: en | ar | it"),
    coll_var("clientEmail", "john.doe@example.com", "Client test email"),
    coll_var("clientOtp", "", "Filled from developmentOtp when the API exposes it"),
    coll_var("clientAccessToken", "", "Client JWT"),
    coll_var("clientRefreshToken", "", "Client refresh token"),
    coll_var("clientUserId", "", "Client user id"),
    coll_var("adminEmail", "admin@enterprise.local", "Seeded admin email"),
    coll_var("adminPassword", "Admin@12345!", "Seeded admin password"),
    coll_var("adminResetOtp", "", "Admin email-change / reset OTP"),
    coll_var("adminAccessToken", "", "Admin JWT — run Admin Login"),
    coll_var("adminRefreshToken", "", "Admin refresh token"),
    coll_var("adminUserId", "", "Admin user id"),
    coll_var("providerEmail", "provider@enterprise.local", "Seeded or created merchant email"),
    coll_var("providerPassword", "Provider@12345!", "Merchant password"),
    coll_var("providerResetOtp", "", "Provider OTP"),
    coll_var("providerAccessToken", "", "Provider JWT — run Provider Login"),
    coll_var("providerRefreshToken", "", "Provider refresh token"),
    coll_var("providerUserId", "", "Provider identity user id"),
    coll_var("providerId", "", "Provider store id from Admin Create Provider"),
    coll_var("adminRoleId", "", "From Create/List admin roles"),
    coll_var("providerRoleId", "", "From Create/List store roles"),
    coll_var("adminStaffId", "", "From Create admin staff"),
    coll_var("providerStaffId", "", "From Create store staff"),
    coll_var("serviceId", "", "From Create/Lookup marketplace service"),
    coll_var("categoryId", "", "From Create/List category"),
    coll_var("productId", "", "From Create/List product"),
    coll_var("orderId", "", "Order id from client or provider order list"),
    coll_var("sessionId", "", "Stripe Checkout session id (cs_...)"),
    coll_var("notificationId", "", "Notification inbox row id (data[].id)"),
    coll_var("cartItemId", "", "Cart line id from Get cart / Add cart item"),
    coll_var("cartId", "", "Shopping cart id (Stripe metadata / webhook)"),
    coll_var("stripeSignature", "", "Stripe-Signature header for webhook tests"),
]


def env(name: str, values: list[dict], eid: str) -> dict:
    return {
        "id": eid,
        "name": name,
        "values": values,
        "_postman_variable_scope": "environment",
    }


def env_val(key: str, value: str) -> dict:
    return {"key": key, "value": value, "enabled": True}


SHARED_ENV = [
    env_val("lang", "en"),
    env_val("blobPublicBaseUrl", BLOB_PLACEHOLDER),
    env_val("mediaContainer", "media"),
    env_val("clientEmail", "john.doe@example.com"),
    env_val("clientOtp", ""),
    env_val("clientAccessToken", ""),
    env_val("clientRefreshToken", ""),
    env_val("adminEmail", "admin@enterprise.local"),
    env_val("adminPassword", "Admin@12345!"),
    env_val("adminResetOtp", ""),
    env_val("adminAccessToken", ""),
    env_val("adminRefreshToken", ""),
    env_val("providerEmail", "provider@enterprise.local"),
    env_val("providerPassword", "Provider@12345!"),
    env_val("providerResetOtp", ""),
    env_val("providerAccessToken", ""),
    env_val("providerRefreshToken", ""),
    env_val("providerId", ""),
    env_val("adminRoleId", ""),
    env_val("providerRoleId", ""),
    env_val("adminStaffId", ""),
    env_val("providerStaffId", ""),
    env_val("serviceId", ""),
    env_val("categoryId", ""),
    env_val("productId", ""),
    env_val("orderId", ""),
    env_val("sessionId", ""),
    env_val("notificationId", ""),
    env_val("cartItemId", ""),
    env_val("cartId", ""),
    env_val("stripeSignature", ""),
]


def count_requests(items) -> int:
    n = 0
    for it in items:
        if "item" in it:
            n += count_requests(it["item"])
        else:
            n += 1
    return n


def main() -> None:
    folders = [
        health,
        client_auth,
        client_profile,
        client_browse,
        client_cart,
        client_payments,
        client_orders,
        client_notifications,
        provider_auth,
        provider_profile,
        provider_store,
        provider_store_products,
        provider_orders,
        provider_notifications,
        provider_roles,
        provider_permissions,
        provider_staff,
        provider_services,
        provider_categories,
        provider_products,
        admin_auth,
        admin_profile,
        admin_notifications,
        admin_providers,
        admin_services,
        admin_roles,
        admin_permissions,
        admin_users,
        admin_clients,
        admin_orders,
        admin_provider_catalog,
        stripe_webhook,
    ]
    n = count_requests(folders)
    collection = {
        "info": {
            "_postman_id": str(uuid4()),
            "name": "Subito Marketplace API",
            "description": COLLECTION_DESC + f"\n\n**{n} requests** covering every v1 controller action plus health probes.",
            "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
        },
        "variable": variables,
        "item": folders,
    }

    coll_path = OUT / "Subito_API.postman_collection.json"
    coll_path.write_text(json.dumps(collection, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    azure = env(
        "Subito - Azure",
        [env_val("baseUrl", AZURE_URL)] + SHARED_ENV,
        "c0a1b2c3-d4e5-6789-abcd-ef0123456789",
    )
    local = env(
        "Subito - Local",
        [env_val("baseUrl", LOCAL_URL), env_val("blobPublicBaseUrl", "http://127.0.0.1:10000/devstoreaccount1")]
        + [v for v in SHARED_ENV if v["key"] != "blobPublicBaseUrl"],
        "a8b7c6d5-e4f3-4210-9876-543210fedcba",
    )
    (OUT / "Subito_Azure.postman_environment.json").write_text(
        json.dumps(azure, indent=2) + "\n", encoding="utf-8"
    )
    (OUT / "Subito_Local.postman_environment.json").write_text(
        json.dumps(local, indent=2) + "\n", encoding="utf-8"
    )

    readme = f"""# Subito Postman collection

Complete, documented collection for **all** Subito API v1 endpoints ({n} requests), with field tables, validation notes, example bodies (optional fields included), multipart **file** uploads, and Azure Blob `imageUrl` examples.

## Files

| File | Purpose |
|---|---|
| `Subito_API.postman_collection.json` | Full collection. Default `baseUrl` is Azure. |
| `Subito_Azure.postman_environment.json` | Azure App Service environment |
| `Subito_Local.postman_environment.json` | Local `http://localhost:5207` + Azurite blob URL |

## Azure URL

API: `{AZURE_URL}`

Swagger: `{AZURE_URL}/swagger`

Set `blobPublicBaseUrl` to the same value as App Setting `BlobStorage__PublicBaseUrl` (e.g. `https://<account>.blob.core.windows.net`). Container defaults to `media`.

## Import

1. Postman → **Import** → select the collection JSON.
2. Import **Subito - Azure** (and optionally **Subito - Local**).
3. Select the Azure environment in the top-right dropdown.
4. Run **05. Admin Auth → Login**, then the rest.

## Image uploads

Create/update JSON does **not** accept a file. After you have an id:

1. Open `Upload … image (file)`
2. Body → form-data → `file` → pick a JPEG/PNG/WebP ≤ 2 MB
3. Send

Returned `imageUrl` looks like:

`https://<account>.blob.core.windows.net/media/providers/{{id}}/logo/{{guid}}.jpg`

## Seeded credentials (dev / typical Azure seed)

- Admin: `admin@enterprise.local` / `Admin@12345!`
- Provider: `provider@enterprise.local` / `Provider@12345!` (if seeded)

On Azure, OTPs arrive by email (`developmentOtp` is usually null).
"""
    (OUT / "README.md").write_text(readme, encoding="utf-8")
    print(f"Wrote {n} requests to {coll_path}")
    print(f"Azure env: {AZURE_URL}")


if __name__ == "__main__":
    main()
