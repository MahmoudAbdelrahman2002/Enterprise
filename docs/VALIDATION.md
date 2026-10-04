# Validation policy

`validation-policy.json` is the shared source for frontend and backend limits. Run `python scripts/generate-validation-policy.py` after changing it. Generated files are committed, so normal builds do not require Python.

| Field | Rules |
| --- | --- |
| Personal names | Required, at most 100 UTF-16 code units, Unicode letters and combining marks, spaces, apostrophes, hyphens and periods; at least one letter. One-character names are allowed. |
| Company names | Required readable single-line text, at most 200 characters. |
| Catalog names | English required; Italian/Arabic optional. Each translation is at most 200 characters (role names: 100). Whitespace-only translations and control characters are rejected. |
| Descriptions | Optional, at most 2,000 characters per translation; multiline text is allowed. |
| Email | At most 254 characters; local part at most 64. Conventional mailbox format, valid domain labels, international domain names supported. No display names, surrounding whitespace, consecutive local-part dots or localhost addresses. |
| Phone | Optional; 7–15 digits, at most 40 characters including formatting. Optional leading +, spaces, hyphens, and balanced nonnested parentheses. This checks structure, not whether a phone number is assigned. |
| New passwords | 8–128 characters, uppercase/lowercase ASCII letters, digit, and symbol; no control characters. Passwords are never trimmed. Change-password requests cannot reuse the supplied current password. Login only checks required/length/text rules, preserving existing credentials. |
| OTP | Exactly six ASCII digits, including leading zeros. `Otp:Length` must match the shared policy; startup checks prevent configuration drift. |
| Service code / SKU | Required, alphanumeric first character, then ASCII letters/numbers/underscore/hyphen. Code length: 100; SKU length: 50. Existing handlers still enforce uniqueness. |
| Product price | 0.01–999,999.99 with at most two decimal places. This marketplace safety cap is configurable in the shared policy and is below the database precision limit. |
| Display order | Whole number from zero to the signed 32-bit database limit. |
| Cart quantity | Whole number 1–999 per line. The cap is a configurable marketplace policy. Repeated additions also respect it. |
| Identifiers | Required IDs must be nonempty GUIDs. Optional IDs can be omitted, but cannot be an empty GUID when supplied. Existing handlers enforce ownership and referenced-record existence. |
| Enums / permissions | Only defined values and permissions belonging to the current portal. Duplicate permissions are rejected. Existing order handlers enforce legal transitions. |
| Filters | Search text at most 200 characters without control characters. Start date must not be after end date. |
| Paging | Page number at least one; page size 1–100; resulting skip must fit the database query's signed integer range. Invalid values return 400 instead of being silently rewritten. |
| Device tokens | Required opaque token, at most 512 characters without whitespace; optional platform is android, ios or web. |
| Refresh tokens | Required opaque text, at most 4,096 characters. Existing token services verify expiry, revocation and portal. |
| Checkout session ID | Required Stripe checkout identifier, at most 255 characters. Existing handlers verify ownership and payment state. |
| Images | JPEG, PNG or WebP; nonempty and at most 2 MiB. Both applications check signatures/container markers against the declared type. Backend checks preserve stream position. These are format checks, not full image decoding or content moderation. |

Explicit active-state mutation booleans and product/order status updates must be supplied; omitted values cannot silently become false or Draft/Pending. Product payloads are checked for null before nested fields are read.

Deactivated providers cannot receive new cart additions or create a checkout. Checkout also rejects invalid quantities/prices in older carts. Product/category activation, cross-provider ownership, duplicate emails/codes/SKUs, and role constraints remain authoritative in existing handlers.

API failures keep the existing `errors` array and add optional `fieldErrors` keyed by property paths. Angular maps these to controls, including localized product names. Field feedback uses translated messages, accessible descriptions and visible form summaries. No inventory model exists, so quantity limits do not imply stock availability.

Tests share `tests/validation-cases.json` for names, email, phone, price and OTP examples. Backend tests cover validator registration, null nested payloads, date ranges, paging overflow, and image stream preservation. Frontend tests cover matching formats, password reuse, server error mapping, and invalid cart quantities.
