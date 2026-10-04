# Catalogue maintenance

Users with the relevant update permission have an explicit Edit action on Provider
Products, Provider Categories, and Admin Services. Each editor loads the full saved
record before displaying the form. Creation and editing share validation and translated
labels. Cancel discards changes; failed saves preserve entered values.

| Resource | Permission | Editable fields | Update route |
| --- | --- | --- | --- |
| Provider product | `ProviderProduct.Update` | Localized name/description, SKU, price, status, category | `PUT /api/v1/provider/categories/{originalCategoryId}/products/{id}` |
| Provider category | `ProviderCategory.Update` | Localized name/description, display order | `PUT /api/v1/provider/categories/{id}` |
| Admin service | `Services.Update` | Code, localized name/description, display order | `PUT /api/v1/admin/services/{id}` |

Name and description fields support English, Arabic and Italian. English names are
required; optional language values follow the existing server fallback rules. Existing
category/service activation and image upload controls remain available.

Product updates may include an optional `categoryId` in the body. The route identifies
the product's original category, and the body identifies the destination. Omitting the
body field preserves the original category for existing API callers. The server checks
product ownership and requires an active destination category belonging to the same
provider. Moving a product preserves its ID, image and references.

Edits preserve existing order item snapshots (`ProductName` and `UnitPrice`). Open baskets
and checkout continue using the existing pricing and availability behavior; this change
does not introduce a price guarantee or a price-change notification policy.

Controller filenames and class names follow [the controller naming convention](CONTROLLER_NAMING.md).
`ProviderProductsController` includes both store-wide listings and category-scoped
maintenance; the prior singular controller has been consolidated into it. Public URLs
and endpoint permissions are retained.

Regression coverage lives in `catalogue-editors.spec.ts`, `CatalogueProductUpdateTests`,
`CatalogueEditingApiTests`, and `ControllerNamingAndRoutesTests`.
