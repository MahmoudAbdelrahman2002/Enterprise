using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Extensions;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Products.Commands.CreateProduct;
using Enterprise.Application.Features.Provider.Products.Commands.DeleteProduct;
using Enterprise.Application.Features.Provider.Products.Commands.DeleteProductImage;
using Enterprise.Application.Features.Provider.Products.Commands.UpdateProduct;
using Enterprise.Application.Features.Provider.Products.Commands.UploadProductImage;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Application.Features.Provider.Products.Queries.GetProviderProductById;
using Enterprise.Application.Features.Provider.Products.Queries.GetProviderProductsList;
using Enterprise.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/categories/{categoryId:guid}/products")]
[RequireProvider]
public sealed class ProviderProductController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.ProviderProduct.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductListItemDto>>>> GetList(
        Guid categoryId,
        [FromQuery] ProductStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetProviderProductsListQuery(categoryId, status), cancellationToken);
        return OkResponse(result, MessageKeys.Product.ListRetrieved);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.ProviderProduct.Read)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetById(
        Guid categoryId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetProviderProductByIdQuery(categoryId, id), cancellationToken);
        return OkResponse(result, MessageKeys.Product.Retrieved);
    }

    [HttpPost]
    [RequirePermission(Permissions.ProviderProduct.Create)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> Create(
        Guid categoryId,
        [FromBody] CreateProductDto product,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateProductCommand(categoryId, product), cancellationToken);
        return CreatedResponse(result, MessageKeys.Product.Created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.ProviderProduct.Update)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> Update(
        Guid categoryId,
        Guid id,
        [FromBody] UpdateProductDto product,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateProductCommand(categoryId, id, product), cancellationToken);
        return OkResponse(result, MessageKeys.Product.Updated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.ProviderProduct.Delete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid categoryId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteProductCommand(categoryId, id), cancellationToken);
        return EmptyResponse(MessageKeys.Product.Deleted);
    }

    [HttpPost("{id:guid}/image")]
    [RequirePermission(Permissions.ProviderProduct.Update)]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> UploadImage(
        Guid categoryId,
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new UploadProductImageCommand(categoryId, id, file.ToImageUploadFile()),
            cancellationToken);
        return OkResponse(result, MessageKeys.Image.Uploaded);
    }

    [HttpDelete("{id:guid}/image")]
    [RequirePermission(Permissions.ProviderProduct.Update)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> DeleteImage(
        Guid categoryId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteProductImageCommand(categoryId, id), cancellationToken);
        return OkResponse(result, MessageKeys.Image.Removed);
    }
}
