# System pagination

Implemented and verified on 4 October 2026.

| Portal | Paged collections |
|---|---|
| Admin | Providers, Services, Clients, Users, Roles, Notifications; existing gated Orders view |
| Provider | Categories, Products, Staff, Roles, Orders, Notifications |
| Client | Homepage services, provider directory, store categories, store products, baskets, Orders, Notifications |

Basket items and order detail items use local pages of 10 rows. The shared order item component covers Client, Provider and the existing Admin detail view. Totals and checkout always use the complete basket or order. Admin order permissions and navigation remain disabled as requested; pagination does not restore `Orders.Read`.

## API contracts

Existing paged list APIs continue to accept `pageNumber` and `pageSize`. These additive endpoints preserve the existing array responses for legacy callers:

| Method | Route | Behavior |
|---|---|---|
| GET | `/api/v1/{admin|provider|client}/notifications/paged` | Authenticated recipient inbox, newest first. Only notifications returned on the requested page are marked read. |
| GET | `/api/v1/client/carts/paged` | Client's nonempty store baskets, newest modified first, with complete item and subtotal data for each returned basket. |
| GET | `/api/v1/client/carts/count` | `{ "totalQuantity": number }` across the client's complete basket collection for the header badge. |
| GET | `/api/v1/client/orders/paged` | Client's orders, newest first; accepts optional `providerId` in addition to paging. |

The new paged endpoints default to page 1 and 20 records. Page size must be 1–100, page number must be at least 1, and the calculated database offset must fit a signed 32-bit integer. Invalid parameters return HTTP 400. Responses use the normal API envelope; `data` contains `items`, `pageNumber`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, and `hasNextPage`.

Database pagination counts the filtered collection before applying Skip/Take and uses a unique ID tie breaker for stable sorting. User and portal scoping applies before counting or retrieving rows. Legacy `GET .../notifications` retains its existing whole-inbox read behavior.

## Frontend behavior

Page navigation offers previous, next and numbered buttons, a total record count, translated labels, an accessible current-page indicator and loading feedback. Paging retains active search and filters. Changing filters returns to page 1. Store category and product pages are independent; selecting a category resets the product page while retaining the selected category label.

Superseded requests and requests belonging to destroyed views are cancelled. Deleting the last record on a page loads the nearest valid page. Keyboard focus returns to the current page control after loading, unless the user has moved focus elsewhere. Controls wrap on narrow screens and have minimum 44-pixel button height.

Role and category form lookups retrieve every server page, so records beyond the first 100 remain selectable. Basket badge refreshes cancel older requests and use the aggregate count endpoint.

## Verification

124 Angular browser tests, 30 backend unit tests and 42 backend integration tests passed. The production frontend build and API build passed. Integration coverage includes recipient isolation, unread state, stable catalogue ordering, basket totals, order ownership, legacy contracts and invalid paging parameters. Browser coverage includes independent store pages, search resets, request cancellation, deletion recovery, focus restoration and full basket/order totals.
