using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Products;
using Enterprise.Application.Features.Products.Commands.AdjustStock;
using Enterprise.Application.Features.Products.Commands.CreateProduct;
using Enterprise.Application.Features.Products.Commands.DeleteProduct;
using Enterprise.Application.Features.Products.Commands.UpdateProduct;
using Enterprise.Application.Features.Products.Queries.GetProductById;
using Enterprise.Application.Features.Products.Queries.GetProductsList;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/products")]
[RequireAdmin]
public sealed class AdminProductsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetList(
        [FromQuery] GetProductsListQuery query, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.Product.ListRetrieved);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AdminProductDto>>> GetById(
        Guid id, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetAdminProductByIdQuery(id), cancellationToken), MessageKeys.Product.Retrieved);

    [HttpPost]
    [RequirePermission("Products.Create")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create(
        [FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await Mediator.Send(command, cancellationToken);
        var body = ApiResponse<ProductDto>.Ok(
            product, Localizer[MessageKeys.Product.Created], StatusCodes.Status201Created, HttpContext.TraceIdentifier);
        return CreatedAtAction(nameof(GetById), new { id = product.Id, version = "1.0" }, body);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Products.Update")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(
        Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(id, request.Name, request.Description, request.Category, request.Price);
        return OkResponse(await Mediator.Send(command, cancellationToken), MessageKeys.Product.Updated);
    }

    [HttpPost("{id:guid}/adjust-stock")]
    [RequirePermission("Products.Update")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> AdjustStock(
        Guid id, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new AdjustStockCommand(id, request.Delta), cancellationToken),
            MessageKeys.Product.StockAdjusted);

    [HttpDelete("{id:guid}")]
    [RequirePermission("Products.Delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return EmptyResponse(MessageKeys.Product.Deleted);
    }
}
