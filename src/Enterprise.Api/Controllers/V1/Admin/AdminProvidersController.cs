using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Extensions;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Providers;
using Enterprise.Application.Features.Providers.Commands.CreateProvider;
using Enterprise.Application.Features.Providers.Commands.DeleteAdminProviderImage;
using Enterprise.Application.Features.Providers.Commands.DeleteProvider;
using Enterprise.Application.Features.Providers.Commands.SetProviderActive;
using Enterprise.Application.Features.Providers.Commands.UpdateProvider;
using Enterprise.Application.Features.Providers.Commands.UploadAdminProviderImage;
using Enterprise.Application.Features.Admin.Providers.Queries.GetAdminProviderCategories;
using Enterprise.Application.Features.Admin.Providers.Queries.GetAdminProviderProducts;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Application.Features.Providers.Queries.GetProviderById;
using Enterprise.Application.Features.Providers.Queries.GetProvidersList;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/providers")]
[RequireAdmin]
public sealed class AdminProvidersController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission("Providers.Read")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProviderAdminListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProviderAdminListItemDto>>>> GetList(
        [FromQuery] GetProvidersListQuery query, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.Provider.ListRetrieved);

    [HttpGet("{id:guid}")]
    [RequirePermission("Providers.Read")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> GetById(
        Guid id, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetProviderByIdQuery(id), cancellationToken), MessageKeys.Provider.Retrieved);

    [HttpPost]
    [RequirePermission("Providers.Create")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> Create(
        [FromBody] CreateProviderCommand command, CancellationToken cancellationToken)
    {
        var provider = await Mediator.Send(command, cancellationToken);
        var body = ApiResponse<ProviderDto>.Ok(
            provider, Localizer[MessageKeys.Provider.Created], StatusCodes.Status201Created, HttpContext.TraceIdentifier);
        return CreatedAtAction(nameof(GetById), new { id = provider.Id, version = "1.0" }, body);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Providers.Update")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> Update(
        Guid id, [FromBody] UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProviderCommand(
            id, request.FirstName, request.LastName, request.CompanyName, request.PhoneNumber, request.ServiceId);
        return OkResponse(await Mediator.Send(command, cancellationToken), MessageKeys.Provider.Updated);
    }

    [HttpPost("{id:guid}/set-active")]
    [RequirePermission("Providers.Update")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> SetActive(
        Guid id, [FromBody] SetProviderActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetProviderActiveCommand(id, request.IsActive), cancellationToken);
        var messageKey = request.IsActive ? MessageKeys.Provider.Activated : MessageKeys.Provider.Deactivated;
        return OkResponse(result, messageKey);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Providers.Delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteProviderCommand(id), cancellationToken);
        return EmptyResponse(MessageKeys.Provider.Deleted);
    }

    [HttpGet("{id:guid}/categories")]
    [RequirePermission("Providers.Read")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDetailDto>>>> GetCategories(
        Guid id,
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new GetAdminProviderCategoriesQuery(id), cancellationToken),
            MessageKeys.Category.ListRetrieved);

    [HttpGet("{id:guid}/products")]
    [RequirePermission("Providers.Read")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductListItemDto>>>> GetProducts(
        Guid id,
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new GetAdminProviderProductsQuery(id), cancellationToken),
            MessageKeys.Product.ListRetrieved);

    [HttpPost("{id:guid}/image")]
    [RequirePermission("Providers.Update")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> UploadImage(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new UploadAdminProviderImageCommand(id, file.ToImageUploadFile()),
            cancellationToken);
        return OkResponse(result, MessageKeys.Image.Uploaded);
    }

    [HttpDelete("{id:guid}/image")]
    [RequirePermission("Providers.Update")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> DeleteImage(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteAdminProviderImageCommand(id), cancellationToken);
        return OkResponse(result, MessageKeys.Image.Removed);
    }
}

public sealed record UpdateProviderRequest(
    string FirstName,
    string LastName,
    string CompanyName,
    string? PhoneNumber,
    Guid? ServiceId = null);

public sealed record SetProviderActiveRequest(bool IsActive);
