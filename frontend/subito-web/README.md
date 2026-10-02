# Subito Web (Angular)

Responsive Angular front end for the Subito marketplace API with three portals:

- **Client storefront** — browse services/stores, cart, Stripe checkout, orders, profile, notifications
- **Provider dashboard** — store, categories, products, orders, roles, staff
- **Admin console** — providers, services, clients, roles, users

## Run locally

1. Start the API (`dotnet run --project src/Enterprise.Api`) on `http://localhost:5207`.
2. From this folder:

```bash
npm install
npm start
```

App: `http://localhost:4200` (proxies `/api` to the API).

Each portal has its own URL and login page. Provider and Admin links are not shown in the storefront footer.

| Portal | URL | Login |
| --- | --- | --- |
| Client (storefront) | http://127.0.0.1:4200/ | http://127.0.0.1:4200/auth/login (email + OTP) |
| Provider | http://127.0.0.1:4200/provider | http://127.0.0.1:4200/provider/login |
| Admin | http://127.0.0.1:4200/admin | http://127.0.0.1:4200/admin/login |

Opening `/provider` or `/admin` without a session redirects to that portal's login page.

Dev admin: `admin@enterprise.local` / `Admin@12345!`

## Theme

Brand logo files:

- Horizontal lockup: `public/assets/logo.png` (headers)
- Standalone mark: `public/assets/logo-mark.png` (compact / favicon)
- Stacked lockup: `public/assets/logo-stacked.png` (optional marketing)

Design tokens (see `src/styles.scss`): navy `#10243A`, turquoise `#00C7B1`, page `#F7F9FC`, surface/card white, muted `#64748B`, border `#E2E8F0`.

Languages: `en` / `ar` / `it` (RTL for Arabic).

## Configuration

- `src/environments/environment.ts` — API base URL (dev uses proxy `/api`)
- `proxy.conf.json` — forwards `/api` → API host
- Stripe success/cancel URLs are configured on the **API** (`SuccessUrl` / `CancelUrl`); keep them on the same origin you use in the browser (`localhost` vs `127.0.0.1`)
