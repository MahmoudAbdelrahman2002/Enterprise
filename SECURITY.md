# Security

This document explains the security posture of the template as explicit reasoning — what is
implemented, what is deliberately not implemented, and why — rather than a checklist of
buzzwords.

## Table of contents

- [Authentication](#authentication)
- [Authorization](#authorization)
- [Password storage](#password-storage)
- [API keys](#api-keys)
- [Refresh tokens](#refresh-tokens)
- [Account lockout](#account-lockout)
- [Transport & headers](#transport--headers)
- [CSRF](#csrf)
- [SQL injection](#sql-injection)
- [XSS](#xss)
- [Rate limiting](#rate-limiting)
- [Secrets & configuration](#secrets--configuration)
- [Hangfire dashboard](#hangfire-dashboard)
- [Logging](#logging)
- [Known gaps / production hardening checklist](#known-gaps--production-hardening-checklist)

## Authentication

Two schemes are registered and accepted interchangeably on the default authorization policy:

- **JWT Bearer** (`Microsoft.AspNetCore.Authentication.JwtBearer`) — short-lived access tokens
  (`Jwt:AccessTokenExpirationMinutes`, default 15 minutes) signed with HMAC-SHA256 using a secret
  key from configuration (`Jwt:Secret`). Validation enforces issuer, audience, lifetime, and
  signing key — nothing is accepted with `ValidateIssuer`/`ValidateAudience`/`ValidateLifetime`
  turned off.
- **API Key** (custom `ApiKeyAuthenticationHandler`) — for service-to-service calls, via the
  `X-Api-Key` header. See [API keys](#api-keys).

Short access-token lifetime is deliberate: if one leaks (log line, browser history, proxy cache),
the exposure window is minutes, not the lifetime of a long-lived token. Session continuity comes
from the refresh token, not from a long access token.

## Authorization

Permission claims (e.g. `Products.Create`, `Products.Delete`) are flattened onto the token's
claims at issuance time from the user's roles → role-permissions graph (see `JwtTokenService` and
`DbSeeder`'s Admin/User role setup). `[RequirePermission("X")]` on a controller action checks a
specific claim, not a role name — so authorization logic never has to special-case "is this user
an Admin" scattered through controllers; it asks "can this token do X," which is the actual
question that matters.

## Password storage

Uses `Microsoft.AspNetCore.Identity.PasswordHasher<T>` — **not** a hand-rolled hash routine and
**not** bare BCrypt/PBKDF2 calls. This class:

- Uses PBKDF2 with HMAC-SHA256, a 128-bit random salt per password, and a configurable
  (currently 100,000+) iteration count.
- Is versioned — the hash format embeds enough metadata that a future .NET version bumping the
  default iteration count can still verify (and optionally rehash) older hashes without a manual
  migration.

Passwords are validated by `RegisterCommandValidator` for minimum length, and character-class
mix (upper/lower/digit) before ever reaching the hasher — rejecting weak passwords is cheaper and
clearer as a 400 validation error than as a silent weak-hash acceptance.

Plaintext passwords are never logged. `LoggingBehavior` logs the *request type name* only, not
serialized request payloads, specifically so a `LoginCommand`/`RegisterCommand` containing a
password never ends up in a log sink.

## API keys

`ApiKey.KeyHash` stores a SHA-256 hash of the secret portion of the key; the raw key
(`ek_live_{prefix}.{secret}`) is shown to the caller **exactly once**, at generation time
(`GenerateApiKeyCommandHandler`'s response), and is never retrievable again — the database alone
cannot be used to authenticate, matching password-hash discipline. The `{prefix}` portion is
stored in plaintext solely so a key can be identified/revoked by an admin without knowing the
secret (`ApiKeySummaryDto`).

Each key has an optional expiration and can be explicitly revoked (`ApiKey.Revoke()`);
`ApiKeyAuthenticationHandler` checks `IsActive` (not expired, not revoked) on every request and
updates `LastUsedAtUtc`/`UsageCount` for auditability.

## Refresh tokens

Refresh tokens are opaque random strings (not JWTs), and only their SHA-256 hash is persisted
(`RefreshToken.TokenHash`) — identical reasoning to passwords and API keys: a stolen database
snapshot does not itself yield usable credentials.

**Rotation on every use**: `RefreshTokenCommandHandler` revokes the presented token and issues a
new access+refresh token pair every single time a refresh happens. A refresh token is single-use.

**Reuse detection**: if a token that has *already been revoked* is presented again, that is
treated as evidence of token theft (a legitimate rotation would never present an already-revoked
token — only an attacker holding a stale copy, or a race, would). The handler responds by
revoking **every other active refresh token belonging to that user**, forcing re-authentication
on all devices, rather than merely rejecting the one request.

A background job (`PurgeExpiredRefreshTokensJob`, scheduled hourly via Hangfire) removes expired
tokens so the table doesn't grow unbounded and so an old, expired token can't be probed
indefinitely.

## Account lockout

`AccountLockoutSettings` (`MaxFailedAccessAttempts`, `LockoutDurationMinutes`) drives
`User.RegisterFailedLogin()`/`IsLockedOut`. After N consecutive failed password checks, the
account is locked for a fixed window regardless of further correct/incorrect attempts, mitigating
online password-guessing. A successful login resets the counter (`ResetFailedLoginCount()`). This
is deliberately per-account, not per-IP — see [Rate limiting](#rate-limiting) for the
complementary per-IP control, which also matters (an attacker spraying many accounts from one IP
should be slowed down even before any single account's lockout threshold is hit).

## Transport & headers

`SecurityHeadersMiddleware` adds, on every response:

| Header | Value | Why |
|---|---|---|
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | Forces HTTPS on every subsequent visit for a year, mitigating SSL-stripping downgrade attacks. Only sent in non-Development environments (localhost dev over HTTP shouldn't get HSTS). |
| `X-Content-Type-Options` | `nosniff` | Stops browsers from MIME-sniffing a response into executing it as something other than its declared `Content-Type` (e.g. treating a JSON error body as HTML/script). |
| `X-Frame-Options` | `DENY` | Prevents the API's HTML surfaces (Swagger UI, Hangfire dashboard) from being framed by another origin — clickjacking mitigation. |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Avoids leaking full request paths/query strings (which could contain resource IDs) to third-party `Referer` headers on cross-origin navigation. |
| `Content-Security-Policy` | API: `default-src 'none'`; omitted on `/swagger` + `/hangfire` | Strict CSP for JSON responses. HTML UIs skip CSP so Swagger/Hangfire scripts and styles can load. |
| `Server` | *(removed)* | `UseKestrel(o => o.AddServerHeader = false)` — don't advertise the exact web server/version to make targeted CVE probing marginally harder ("security through obscurity" is not a primary control here, but it's free to do and has no downside). |

HTTPS redirection (`UseHttpsRedirection`) is enabled; the API is expected to sit behind TLS
termination (reverse proxy/load balancer) in production, with `Forwarded Headers` middleware
configured for that topology (not included by default since it's deployment-specific).

## CSRF

**Not implemented with anti-forgery tokens, deliberately, not by oversight.**

Classic CSRF exploits the browser's automatic attachment of ambient credentials (cookies) to
cross-site requests. This API is stateless Bearer-token authentication: the JWT/API key travels
in an `Authorization`/`X-Api-Key` header that a browser **never** attaches automatically to a
cross-origin request the way it does a cookie. A malicious page cannot forge a request that
carries the victim's token unless it can already read that token from somewhere JavaScript has
access to — at which point the vulnerability is XSS-driven token theft, not CSRF, and the
mitigation for that is [XSS hardening](#xss) and short-lived tokens, not an anti-forgery token.

Anti-forgery middleware exists specifically to protect *cookie-authenticated* form posts; bolting
it onto a token API that doesn't use cookie auth would be adding a defense for a threat model
this API doesn't have — cargo-culting a checklist item rather than reasoning about the actual
attack surface.

**If this template is ever adapted to use cookie-based auth** (e.g. serving a first-party SPA
that stores the token in an `HttpOnly` cookie instead of `localStorage`), anti-forgery tokens
(`Microsoft.AspNetCore.Antiforgery`) would need to be added at that point — that combination is
exactly the case CSRF protection exists for.

## SQL injection

All data access goes through EF Core's LINQ provider and parameterized queries via
`GenericRepository<T>`/`ISpecification<T>` — there is no raw string-concatenated SQL anywhere in
the template. EF Core parameterizes every value that flows into a generated query, so user input
(search terms, filters, sort fields validated against allow-lists in specifications)
cannot break out of its parameter slot.

## XSS

This is a pure JSON API (no server-rendered HTML views), which removes the most common XSS
vector by construction — there is no Razor view interpolating user input into an HTML response.
`X-Content-Type-Options: nosniff` (and CSP on JSON responses only) are defense-in-depth.
`/swagger` and `/hangfire` omit CSP so their UIs can load. Consumers building a frontend
against this API remain responsible for escaping/encoding when rendering any API-supplied
string into their own DOM.

## Rate limiting

ASP.NET Core's built-in rate limiter (`Microsoft.AspNetCore.RateLimiting`, no third-party
package):

- **Global policy** — fixed window, 100 requests/minute per client IP, applied to the pipeline by
  default. Generous enough not to interfere with normal product-browsing/paging traffic.
- **`auth` policy** — fixed window, 5 requests/minute per client IP, applied only to
  `/api/v{version}/auth/*` (register/login/refresh/revoke/api-key generation). These are
  precisely the endpoints an attacker would target for credential stuffing or brute-forcing, so
  they get a materially tighter budget than general API traffic rather than sharing the global
  one.

Rate limiting is per-IP, which is complementary to (not a replacement for) the per-account
[lockout policy](#account-lockout) above — one slows a single attacker hammering many accounts,
the other stops repeated guesses against one account regardless of source IP/botnet size.

## Secrets & configuration

- `Jwt:SecretKey` in `appsettings.Development.json` is a development-only placeholder value,
  intentionally checked in since it's local-only and worthless outside a dev machine. **It must
  be overridden** in any real environment via environment variables, `dotnet user-secrets`, or a
  secret manager (Azure Key Vault / AWS Secrets Manager) — never checked into source control for
  a real deployment.
- Connection strings in `appsettings.json` default to empty; `appsettings.Development.json`
  supplies a LocalDB connection string for local development only.
- The seeded admin account's credentials (`SeedData:AdminEmail`/`AdminPassword` in
  `appsettings.Development.json`) are for local development convenience only — the seeder should
  either be disabled or reconfigured with a strong, out-of-band password before ever seeding a
  non-development database.

## Hangfire dashboard

`/hangfire` renders an HTML dashboard and, because it's meant for a human to browse to directly,
cannot be protected by the JWT/API-key Bearer schemes the rest of the API uses (a browser
navigating to a URL doesn't attach an `Authorization` header). `HangfireDashboardAuthorizationFilter`
restricts access to loopback (localhost) requests as a conservative baseline for local
development. **In a real deployment**, this should be replaced with a network-level control (put
it behind a VPN, an internal-only load balancer listener, or an IP allowlist at the reverse
proxy) rather than relying on the loopback check alone, since a reverse-proxied production
deployment may make every request appear to originate from the proxy's loopback address.

## Logging

Serilog structured logging enriches every log line with `CorrelationId` (from
`CorrelationIdMiddleware`), machine name, thread id, and environment. Request/response bodies are
**not** logged wholesale (only the MediatR request *type name* via `LoggingBehavior`), which is
also why credentials never end up in a log sink incidentally. Exceptions logged by
`GlobalExceptionHandler`/`UnhandledExceptionBehavior` include the full exception for
diagnostics server-side, but the HTTP response body only ever includes exception details when
`IsDevelopment()` is true — production responses get a generic `ProblemDetails` message plus a
`traceId` to correlate with server-side logs, not a stack trace.

## Known gaps / production hardening checklist

Documented explicitly rather than silently absent:

- **No email verification / password reset flow** — out of scope for this template (it
  demonstrates the auth *architecture*, not a full account-management product surface).
- **No refresh-token device/session listing UI** — the data model (`RefreshToken` per issuance)
  supports building one; it isn't exposed as an endpoint here.
- **No WAF / DDoS-layer protection** — expected to be provided by the hosting
  platform/CDN/reverse proxy in front of this API, not by the application itself.
- **CSP is omitted on `/swagger` and `/hangfire`** — those HTML UIs need scripts/styles that a
  strict API CSP would block. Prefer disabling those UIs outside Development rather than
  exposing them with a weak CSP on the public internet.
