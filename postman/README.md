# Subito Postman collection

Complete, documented collection for **all** Subito API v1 endpoints (143 requests), with field tables, validation notes, example bodies (optional fields included), multipart **file** uploads, and Azure Blob `imageUrl` examples.

## Files

| File | Purpose |
|---|---|
| `Subito_API.postman_collection.json` | Full collection. Default `baseUrl` is Azure. |
| `Subito_Azure.postman_environment.json` | Azure App Service environment |
| `Subito_Local.postman_environment.json` | Local `http://localhost:5207` + Azurite blob URL |

## Azure URL

API: `https://subito-api-hda8ggdbhehqa9dk.italynorth-01.azurewebsites.net`

Swagger: `https://subito-api-hda8ggdbhehqa9dk.italynorth-01.azurewebsites.net/swagger`

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

`https://<account>.blob.core.windows.net/media/providers/{id}/logo/{guid}.jpg`

## Seeded credentials (dev / typical Azure seed)

| Portal | Email | Password |
|---|---|---|
| Admin (system) | `admin@enterprise.local` | `Admin@12345!` |
| Admin staff | `support@enterprise.local` | `Admin@12345!` |
| Admin staff | `ops@enterprise.local` | `Admin@12345!` |
| Provider owner | `provider@enterprise.local` | `Provider@12345!` |
| Provider owner | `pharmacy@enterprise.local` | `Provider@12345!` |
| Provider staff | `manager@demo-restaurant.local` | `Staff@12345!` |
| Provider staff | `cashier@demo-restaurant.local` | `Staff@12345!` |
| Provider staff | `clerk@green-pharmacy.local` | `Staff@12345!` |
| Client | `client@enterprise.local` | `Client@12345!` |
| Client | `client2@enterprise.local` … `client4@…` | `Client@12345!` |

On Azure, OTPs arrive by email (`developmentOtp` is usually null).
