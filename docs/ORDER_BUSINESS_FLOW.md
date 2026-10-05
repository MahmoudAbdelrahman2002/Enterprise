# Order business flow

Updated 5 October 2026. The active workflow is **New -> Preparing -> Ready**.

## Checkout and order creation

Each Client basket belongs to one Provider. Hosted Stripe Checkout collects payment for that basket. A verified paid Checkout session creates one order, whether confirmed through a signed webhook or the authenticated Client confirmation endpoint. Session idempotency prevents duplicate orders.

Order items are immutable snapshots of product name, price and paid quantity. Paid quantities are consumed from the basket while later additions remain. Orders retain their own totals and history independently of catalogue edits.

Every newly created order starts as **New**. Cancelling an unpaid Stripe Checkout session leaves the basket available; this is separate from order cancellation, which is no longer supported.

## Status workflow

| Status | API value | Meaning | Next stage |
|---|---|---|---|
| New | 0 | Paid order awaiting preparation | Preparing |
| Preparing | 2 | Provider is preparing the order | Ready |
| Ready | 3 | Preparation is complete | None |

Only an authorized Provider with `ProviderOrder.Update` may advance its own orders. Transitions must proceed one stage at a time. Skipped stages, backward transitions and repeated statuses return HTTP 409. Removed status values and names are invalid and return HTTP 400. Ready is terminal.

Provider order detail shows only the next permitted stage. Client order pages display the current stage and offer no cancellation action. Admin order navigation and permissions remain disabled as requested.

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/client/orders/paged` | Client order history with paging and optional Provider filter |
| GET | `/api/v1/client/orders/{orderId}` | Owned order detail |
| GET | `/api/v1/client/orders/by-session/{sessionId}` | Owned order for a Checkout session |
| POST | `/api/v1/client/orders/confirm-session/{sessionId}` | Confirm a paid owned session idempotently |
| GET | `/api/v1/provider/orders` | Paged active store orders |
| GET | `/api/v1/provider/orders/{orderId}` | Authorized store order detail |
| PATCH | `/api/v1/provider/orders/{orderId}/status` | Advance to Preparing or Ready |

The former `POST /api/v1/client/orders/{orderId}/cancel` endpoint is removed. Order status notifications link to the exact order and are emitted only after successful transitions.

## Existing records

Migration `SimplifyOrderWorkflow` preserves existing orders and their item snapshots:

- Pending (0) and Accepted (1) become New (0).
- Preparing (2) and Ready (3) retain their existing values.
- Completed (4) becomes Ready (3).
- Previously cancelled records remain read-only Client history, shown as **Historical order** with `isHistorical: true`; they do not appear in the active Provider queue and cannot reenter the workflow.

`PreviousStatus` retains legacy Accepted, Completed and Cancelled values for history and migration rollback. Historical records use terminal storage value 3, but their historical flag takes precedence over the displayed phase. No records, totals, item snapshots, notes or Checkout references are deleted. Existing Preparing/Ready numeric values are retained to avoid reinterpreting stored records.
