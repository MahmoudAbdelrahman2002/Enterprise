# Subito marketplace UI

The customer route flow is preserved: service discovery → stores → store products → product details → one basket per store → Stripe hosted checkout → session confirmation/order polling → orders. Global search still passes `?q=` to service discovery; product search stays within a store.

The implementation includes navy/turquoise styling, a discovery hero, responsive cards, contained product imagery, shared SVG icons and uploads, accessible confirmation dialogs, localized prices, basket mutation states and clear-basket confirmation, payment recovery states, translated management screens, permission-aware dashboards and backend-aligned order status choices.

## Changed files

Shared/core:

- `.gitignore`, `src/index.html`, `src/styles.scss`
- `src/app/layouts/storefront-layout/storefront-layout.component.ts`
- `src/app/layouts/dashboard-layout/dashboard-layout.component.ts`
- `src/app/shared/components/icon/icon.component.ts` (new)
- `src/app/shared/components/image-upload/image-upload.component.ts` (new)
- `src/app/shared/directives/tooltip.directive.ts` (new)
- `src/app/shared/pipes/money.pipe.ts`, `money.pipe.spec.ts` (new)
- `src/app/shared/components/confirm-host/confirm-host.component.ts`
- `src/app/shared/components/search-field/search-field.component.ts`
- `src/app/shared/components/pagination/pagination.component.ts`
- `src/app/shared/components/empty-state/empty-state.component.ts`
- `src/app/shared/components/logo/logo.component.ts` (intrinsic mark proportions)
- `src/app/core/services/auth.service.ts`, `cart.service.ts`, `cart.service.spec.ts` (new), `orders.service.ts`, `confirm.service.ts`, `i18n.service.ts`
- `public/assets/i18n/en.json`, `ar.json`, `it.json`

Components under `src/app/features/client/`:

- `home/home.component.ts`, `providers/providers-list.component.ts`, `store/store.component.ts`, `product/product-detail.component.ts`
- `cart/cart.component.ts`, `cart/carts-list.component.ts`
- `orders/orders-list.component.ts`, `orders/order-detail.component.ts`
- `payment/payment-success.component.ts`, `payment/payment-cancel.component.ts`
- `auth/client-login.component.ts`, `auth/client-register.component.ts`
- `profile/client-profile.component.ts`, `notifications/client-notifications.component.ts`

Components under `src/app/features/provider/`:

- `dashboard/provider-shell.component.ts`, `dashboard/provider-home.component.ts`
- `auth/provider-login.component.ts`, `auth/provider-forgot.component.ts`
- `store/provider-store.component.ts`, `categories/provider-categories.component.ts`, `products/provider-products.component.ts`
- `orders/provider-orders.component.ts`, `roles/provider-roles.component.ts`, `staff/provider-staff.component.ts`
- `profile/provider-profile.component.ts`, `notifications/provider-notifications.component.ts`

Components under `src/app/features/admin/`:

- `dashboard/admin-shell.component.ts`, `dashboard/admin-home.component.ts`
- `auth/admin-login.component.ts`, `auth/admin-forgot.component.ts`
- `providers/admin-providers.component.ts`, `providers/admin-provider-detail.component.ts`, `services/admin-services.component.ts`
- `clients/admin-clients.component.ts`, `roles/admin-roles.component.ts`, `users/admin-users.component.ts`, `orders/admin-orders.component.ts`
- `profile/admin-profile.component.ts`, `notifications/admin-notifications.component.ts`

Verification tooling: `scripts/check-ui.cjs` (new). Application dependencies and package lock are unchanged. Playwright is installed only in the ignored `.ui-check` directory.

## Verification and reproduction

The production build passes the existing budgets, with an approximately 382.8 kB initial bundle. All 17 Jasmine/Karma tests pass, including new checks for store-specific basket clearing, quantity badge refresh, error propagation, hosted checkout and currency localization.

Browser checks use the production output and API fixtures in Chrome. They cover English, Arabic and Italian at 390, 768, 1024 and 1440 pixels; page overflow, labels, direction, mirrored chevrons, management forms, authentication screens, basket changes, search, checkout redirect, confirmation focus trapping/restoration, keyboard tooltips and payment recovery. Screenshots and the final JSON report are in `.ui-check/artifacts`.

Final result: **444 route/language/viewport checks passed**, with no page errors or page-level horizontal overflow. Management form labels and overflow checks passed in all languages and widths. Basket, search, hosted-checkout redirect, dialog keyboard behavior and payment retry checks passed against fixtures. All new template translation keys exist in all three dictionaries.

```powershell
$env:NODE_OPTIONS='--use-system-ca'
npm.cmd run build:prod
$env:CHROME_BIN='C:\Program Files\Google\Chrome\Application\chrome.exe'
npm.cmd test -- --watch=false --browsers=ChromeHeadless
npm.cmd install --prefix .ui-check --no-package-lock --no-save playwright
node scripts/check-ui.cjs
```

PowerShell blocks `npm.ps1` on this machine, so use `npm.cmd`. Node requires `--use-system-ca` to trust the machine's network certificate for Google font inlining; certificate verification remains enabled. Google fonts retain system font fallbacks.

## Data and verification limits

Amounts use **USD**, matching both checked-in Stripe configurations. DTOs currently lack currency; update `CHECKOUT_CURRENCY` in `money.pipe.ts` if the backend configuration changes. Backend basket/order totals remain authoritative.

Ratings, discounts, previous prices, delivery estimates/fees and favorites are unsupported by current DTOs. No invented values or inactive controls were added. The existing logo assets, portal tokens, refresh interceptor, route guards, API envelope/list normalization and multipart upload workflows are retained.

Live OTP delivery, password authentication, management uploads/writes, Stripe payment and webhook settlement were not exercised against a real backend. Browser checks use fixtures to avoid external writes and payments.
