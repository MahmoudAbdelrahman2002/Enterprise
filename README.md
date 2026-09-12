# Enterprise Clean Architecture API

ASP.NET Core (.NET 10) API with Clean Architecture, CQRS (MediatR), and **ASP.NET Core Identity** authentication.

## Auth (Phase 1)

| Portal | Base route | Login |
|---|---|---|
| Client | `/api/v1/client/...` | Email + OTP |
| Admin | `/api/v1/admin/...` | Email + password |

Dev admin: `admin@enterprise.local` / `Admin@12345!` (name: System Administrator)

Docs:

- [`docs/AUTH_FLOW.md`](docs/AUTH_FLOW.md) — endpoint reference
- [`docs/AUTH_STEP_BY_STEP.md`](docs/AUTH_STEP_BY_STEP.md) — how to try it
- [`ARCHITECTURE.md`](ARCHITECTURE.md) / [`SECURITY.md`](SECURITY.md) — may still describe older custom auth in places

## Run

```bash
dotnet restore
dotnet run --project src/Enterprise.Api
```

Development auto-migrates and seeds. If you had the old schema, **drop/recreate** database `EnterpriseDb` first.

OTP emails: configure `Smtp` in appsettings, or leave empty and read codes from logs.

## Solution

```text
src/
  Enterprise.Domain/
  Enterprise.Application/
  Enterprise.Infrastructure/   # Identity, EF Core, JWT, email/OTP
  Enterprise.Api/
```

## Next (not implemented yet)

Roles module (`nameEn` / `nameAr`), permissions admin APIs, products auth cleanup.
