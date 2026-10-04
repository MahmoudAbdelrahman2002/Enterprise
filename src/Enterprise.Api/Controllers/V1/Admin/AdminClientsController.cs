using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Admin.Clients;
using Enterprise.Application.Features.Admin.Clients.Commands.SetAdminClientActive;
using Enterprise.Application.Features.Admin.Clients.Queries.GetAdminClientById;
using Enterprise.Application.Features.Admin.Clients.Queries.GetAdminClients;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/clients")]
[RequireAdmin]
public sealed class AdminClientsController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.Clients.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AdminClientDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminClientDto>>>> GetList(
        [FromQuery] GetAdminClientsQuery query,
        CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.ClientUser.ListRetrieved);

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.Clients.Read)]
    [ProducesResponseType(typeof(ApiResponse<AdminClientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AdminClientDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new GetAdminClientByIdQuery(id), cancellationToken),
            MessageKeys.ClientUser.Retrieved);

    [HttpPost("{id:guid}/set-active")]
    [RequirePermission(Permissions.Clients.Update)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> SetActive(
        Guid id,
        [FromBody] SetAdminClientActiveRequest request,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new SetAdminClientActiveCommand(id, request.IsActive), cancellationToken);
        return EmptyResponse(request.IsActive ? MessageKeys.ClientUser.Activated : MessageKeys.ClientUser.Deactivated);
    }
}

public sealed record SetAdminClientActiveRequest([property: System.Text.Json.Serialization.JsonRequired] bool IsActive);
