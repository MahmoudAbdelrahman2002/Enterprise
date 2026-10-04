using Asp.Versioning;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.product;
using Enterprise.Application.Features.Client.product.Queries.GetAllClientProductQuery;
using Enterprise.Application.Features.Client.product.Queries.GetClientProductByCategory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/categories/{categoryId:guid}/products")]
[AllowAnonymous]
public sealed class ClientCategoryProductsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClientProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<ClientProductDto>>>> GetList(
        [FromRoute] Guid categoryId,
        [FromQuery] string? searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var request = new GetAllClientProductQuery
        {
            CategoryId = categoryId,
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        return OkResponse(
            await Mediator.Send(request, cancellationToken),
            MessageKeys.Product.ListRetrieved);
    }

    [HttpGet("{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClientProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClientProductDto>>> GetById(
        [FromRoute] Guid categoryId,
        [FromRoute] Guid productId,
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new GetClientProductByCategoryQuery(categoryId, productId), cancellationToken),
            MessageKeys.Product.Retrieved);
}
