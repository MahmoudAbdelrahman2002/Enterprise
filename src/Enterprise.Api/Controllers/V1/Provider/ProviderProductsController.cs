using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Application.Features.Provider.Products.Queries.GetProviderStoreProducts;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/products")]
[RequireProvider]
public sealed class ProviderProductsController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.ProviderProduct.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductListItemDto>>>> GetList(
        [FromQuery] GetProviderStoreProductsQuery query,
        CancellationToken cancellationToken = default) =>
        OkResponse(
            await Mediator.Send(query, cancellationToken),
            MessageKeys.Product.ListRetrieved);
}
