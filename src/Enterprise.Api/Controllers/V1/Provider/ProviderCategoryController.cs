using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Categories.Commands.CreateCategory;
using Enterprise.Application.Features.Provider.Categories.Commands.DeleteCategoryCommand;
using Enterprise.Application.Features.Provider.Categories.Commands.UpdateCategoryCommand;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Application.Features.Provider.Categories.Queries.GetCategoriesListQuery;
using Enterprise.Application.Features.Provider.Categories.Queries.GetCategoryById;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/categories")]
[RequireProvider]
public sealed class ProviderCategoryController : ApiControllerBase
{
    [HttpPost]
    [RequirePermission(Permissions.ProviderCategory.Create)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [RequirePermission(Permissions.ProviderCategory.Create)]
    public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> Create(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedResponse(result, MessageKeys.Category.Created);
    }
    [HttpGet("{id}")]
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
    [HttpPut("{id}")]
    [RequirePermission(Permissions.ProviderCategory.Update)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryDto command,
        CancellationToken cancellationToken)
    {   
        var result = await Mediator.Send(new UpdateCategoryCommand(id, command), cancellationToken);
        return OkResponse(result, MessageKeys.Category.Updated);
    }
[HttpDelete("{id:guid}")]
[RequirePermission(Permissions.ProviderCategory.Delete)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
public async Task<ActionResult<ApiResponse<object?>>> Delete(
    Guid id, CancellationToken cancellationToken)
{
    await Mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
    return EmptyResponse(MessageKeys.Category.Deleted);
}
[HttpGet]
[RequirePermission(Permissions.ProviderCategory.Read)]
[ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDetailDto>>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDetailDto>>>> GetList(
    CancellationToken cancellationToken)
{
    var result = await Mediator.Send(new GetCategoriesListQuery(), cancellationToken);
    return OkResponse(result, MessageKeys.Category.ListRetrieved);
}
}