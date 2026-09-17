using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Admin.Services.Commands.CreateMarketplaceService;
using Enterprise.Application.Features.Admin.Services.Commands.DeleteMarketplaceService;
using Enterprise.Application.Features.Admin.Services.Commands.SetMarketplaceServiceActive;
using Enterprise.Application.Features.Admin.Services.Commands.UpdateMarketplaceService;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Application.Features.Admin.Services.Queries.GetAdminServiceById;
using Enterprise.Application.Features.Admin.Services.Queries.GetAdminServicesList;
using Enterprise.Application.Features.Admin.Services.Queries.GetServicesLookup;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/services")]
[RequireAdmin]
public sealed class AdminServicesController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission("Services.Read")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<MarketplaceServiceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<MarketplaceServiceDto>>>> GetList(
        [FromQuery] GetAdminServicesListQuery query, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.Service.ListRetrieved);

    [HttpGet("lookup")]
    [RequirePermission("Services.Read")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MarketplaceServiceLookupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MarketplaceServiceLookupDto>>>> GetLookup(
        CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetServicesLookupQuery(), cancellationToken), MessageKeys.Service.ListRetrieved);

    [HttpGet("{id:guid}")]
    [RequirePermission("Services.Read")]
    [ProducesResponseType(typeof(ApiResponse<MarketplaceServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MarketplaceServiceDto>>> GetById(
        Guid id, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetAdminServiceByIdQuery(id), cancellationToken), MessageKeys.Service.Retrieved);

    [HttpPost]
    [RequirePermission("Services.Create")]
    [ProducesResponseType(typeof(ApiResponse<MarketplaceServiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MarketplaceServiceDto>>> Create(
        [FromBody] CreateMarketplaceServiceCommand command, CancellationToken cancellationToken)
    {
        var service = await Mediator.Send(command, cancellationToken);
        var body = ApiResponse<MarketplaceServiceDto>.Ok(
            service, Localizer[MessageKeys.Service.Created], StatusCodes.Status201Created, HttpContext.TraceIdentifier);
        return CreatedAtAction(nameof(GetById), new { id = service.Id, version = "1.0" }, body);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Services.Update")]
    [ProducesResponseType(typeof(ApiResponse<MarketplaceServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MarketplaceServiceDto>>> Update(
        Guid id, [FromBody] UpdateMarketplaceServiceRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateMarketplaceServiceCommand(
            id, request.Code, request.Name, request.Description, request.DisplayOrder);
        return OkResponse(await Mediator.Send(command, cancellationToken), MessageKeys.Service.Updated);
    }

    [HttpPost("{id:guid}/set-active")]
    [RequirePermission("Services.Update")]
    [ProducesResponseType(typeof(ApiResponse<MarketplaceServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MarketplaceServiceDto>>> SetActive(
        Guid id, [FromBody] SetServiceActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetMarketplaceServiceActiveCommand(id, request.IsActive), cancellationToken);
        var messageKey = request.IsActive ? MessageKeys.Service.Activated : MessageKeys.Service.Deactivated;
        return OkResponse(result, messageKey);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Services.Delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteMarketplaceServiceCommand(id), cancellationToken);
        return EmptyResponse(MessageKeys.Service.Deleted);
    }
}

public sealed record UpdateMarketplaceServiceRequest(
    string Code,
    LocalizedText Name,
    LocalizedText? Description,
    int DisplayOrder = 0);

public sealed record SetServiceActiveRequest(bool IsActive);
