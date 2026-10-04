# Controller naming

Controller filenames match their class names and use PascalCase with the `Controller` suffix.
Portal controllers use `<Role><Resource>Controller`: `AdminServicesController`,
`ProviderProductsController`, and `ClientCartsController`.

Resource collections use plural names. `Staff` is a collective noun. Singleton surfaces
retain `Auth`, `Profile`, and `Store`. Integration controllers identify the integration
and resource, such as `StripeWebhooksController`.

When a separate controller represents a narrower collection, include its scope:
`ClientCategoryProductsController`, `ClientProviderProductsController`, and
`ClientProviderCategoriesController`. Individual record actions belong to the same
resource controller rather than a separate singular or `Detail` controller.

`ProviderProductsController` combines the store product list and category-scoped product
maintenance. `ClientCartsController` combines the cart list and provider-scoped cart
actions. Explicit action routes preserve their existing URLs and permissions.

Changing a filename or class name must preserve the public route unless an API change
is explicitly intended. `ControllerNamingAndRoutesTests` checks the naming convention,
combined route coverage, and duplicate route declarations.
