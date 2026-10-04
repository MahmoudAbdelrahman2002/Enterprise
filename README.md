# Enterprise Clean Architecture API

ASP.NET Core (.NET 10) API with Clean Architecture, CQRS (MediatR), and **ASP.NET Core Identity** authentication.

## Auth (Phase 1)

| Portal | Base route | Login |
|---|---|---|
| Client | `/api/v1/client/...` | Email + OTP |
| Admin | `/api/v1/admin/...` | Email + password |

Dev admin: `admin@enterprise.local` / `Admin@12345!` (name: System Administrator)

Docs:

- [`docs/DEVELOPER_WORKFLOW.md`](docs/DEVELOPER_WORKFLOW.md) — junior guide: request flow + Admin / Provider / Client views
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

Backend file logs are written to `src/Enterprise.Api/log/log-YYYYMMDD.txt` locally
(`{ContentRoot}/log` when deployed). The file threshold is `Warning`: warnings,
errors, and fatal events are recorded, including slow-request warnings above 500 ms.
The folder is created during build and at runtime. Files roll daily, with the latest 14
retained. Console logging keeps its configured level.

## Local Stripe checkout

Configure a Stripe sandbox secret key locally before testing checkout. From the
repository root, replace the placeholder below with your own key:

```powershell
dotnet user-secrets set "Stripe:SecretKey" "YOUR_STRIPE_TEST_SECRET_KEY" --project src/Enterprise.Api
dotnet user-secrets set "Stripe:SuccessUrl" "http://127.0.0.1:4200/payment/success?session_id={CHECKOUT_SESSION_ID}" --project src/Enterprise.Api
dotnet user-secrets set "Stripe:CancelUrl" "http://127.0.0.1:4200/payment/cancel" --project src/Enterprise.Api
```

Restart the API with `dotnet run --project src/Enterprise.Api --launch-profile http`.
Keep the secret key out of frontend code and committed settings. API keys are available
in the [Stripe Dashboard](https://dashboard.stripe.com/test/apikeys).
Webhook processing also requires a separate `Stripe:WebhookSecret`.
Open the frontend at `http://127.0.0.1:4200` when using these return URLs so
checkout returns to the same browser origin and preserves your login.

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

## Local image uploads

Development uses Azure Blob Storage through Azurite. Start the local blob emulator before uploading service, provider, category, or product images:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start-local-storage.ps1
```

The script installs Azurite locally if needed and starts it in the background on port 10000. Uploaded files persist in `.build-check/azurite/data`; keep that directory to retain local images. Run the command again after restarting Windows. Node.js and npm are required. Production uses the configured Azure storage account.
