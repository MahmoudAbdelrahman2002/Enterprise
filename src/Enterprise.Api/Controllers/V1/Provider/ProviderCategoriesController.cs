using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Categories.Commands.CreateCategory;
using Enterprise.Application.Features.Provider.Categories.Commands.DeleteCategory;
using Enterprise.Application.Features.Provider.Categories.Commands.DeleteCategoryImage;
using Enterprise.Application.Features.Provider.Categories.Commands.SetCategoryActive;
using Enterprise.Application.Features.Provider.Categories.Commands.UpdateCategory;
using Enterprise.Application.Features.Provider.Categories.Commands.UploadCategoryImage;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Application.Features.Provider.Categories.Queries.GetCategoriesList;
using Enterprise.Application.Features.Provider.Categories.Queries.GetCategoriesLookup;
using Enterprise.Application.Features.Provider.Categories.Queries.GetCategoryById;
using Enterprise.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/categories")]
[RequireProvider]
public sealed class ProviderCategoriesController : ApiControllerBase
{
    [HttpPost]
    [RequirePermission(Permissions.ProviderCategory.Create)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> Create(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedResponse(result, MessageKeys.Category.Created);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.ProviderCategory.Read)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> Get(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return OkResponse(result, MessageKeys.Category.Retrieved);
    }

    [HttpGet]
    [RequirePermission(Permissions.ProviderCategory.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDetailDto>>>> GetList(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCategoriesListQuery(isActive), cancellationToken);
        return OkResponse(result, MessageKeys.Category.ListRetrieved);
    }

    [HttpGet("lookup")]
    [RequirePermission(Permissions.ProviderCategory.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryLookupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryLookupDto>>>> Lookup(
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCategoriesLookupQuery(), cancellationToken);
        return OkResponse(result, MessageKeys.Category.ListRetrieved);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.ProviderCategory.Update)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(id, request.Name, request.Description, request.DisplayOrder);
        var result = await Mediator.Send(command, cancellationToken);
        return OkResponse(result, MessageKeys.Category.Updated);
    }

    [HttpPost("{id:guid}/set-active")]
    [RequirePermission(Permissions.ProviderCategory.Update)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> SetActive(
        [FromRoute] Guid id,
        [FromBody] SetCategoryActiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetCategoryActiveCommand(id, request.IsActive), cancellationToken);
        var messageKey = request.IsActive ? MessageKeys.Category.Activated : MessageKeys.Category.Deactivated;
        return OkResponse(result, messageKey);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.ProviderCategory.Delete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        [FromRoute] Guid id,
        [FromQuery] bool deleteRelatedProducts = false,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new DeleteCategoryCommand(id, deleteRelatedProducts), cancellationToken);
        return EmptyResponse(MessageKeys.Category.Deleted);
    }

    [HttpPost("{id:guid}/image")]
    [RequirePermission(Permissions.ProviderCategory.Update)]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> UploadImage(
        [FromRoute] Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UploadCategoryImageCommand(id, file.ToImageUploadFile()), cancellationToken);
        return OkResponse(result, MessageKeys.Image.Uploaded);
    }

    [HttpDelete("{id:guid}/image")]
    [RequirePermission(Permissions.ProviderCategory.Update)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> DeleteImage(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteCategoryImageCommand(id), cancellationToken);
        return OkResponse(result, MessageKeys.Image.Removed);
    }
}

public sealed record UpdateCategoryRequest(
    LocalizedText Name,
    LocalizedText? Description = null,
    int DisplayOrder = 0);

public sealed record SetCategoryActiveRequest([property: System.Text.Json.Serialization.JsonRequired] bool IsActive);
