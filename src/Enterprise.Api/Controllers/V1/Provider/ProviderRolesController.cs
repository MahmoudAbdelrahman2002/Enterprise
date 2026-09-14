using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Roles.Commands.CreateProviderRole;
using Enterprise.Application.Features.Provider.Roles.Commands.DeleteProviderRole;
using Enterprise.Application.Features.Provider.Roles.Commands.UpdateProviderRole;
using Enterprise.Application.Features.Provider.Roles.Queries.GetProviderRoleById;
using Enterprise.Application.Features.Provider.Roles.Queries.GetProviderRolesList;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/roles")]
[RequireProvider]
public sealed class ProviderRolesController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission("ProviderRoles.Read")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RoleListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<RoleListItemDto>>>> GetList(
        [FromQuery] GetProviderRolesListQuery query, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.Role.ListRetrieved);

    [HttpGet("{id:guid}")]
    [RequirePermission("ProviderRoles.Read")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> GetById(
        Guid id, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetProviderRoleByIdQuery(id), cancellationToken), MessageKeys.Role.Retrieved);

    [HttpPost]
    [RequirePermission("ProviderRoles.Create")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> Create(
        [FromBody] CreateProviderRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await Mediator.Send(command, cancellationToken);
        var body = ApiResponse<RoleDetailDto>.Ok(
            role, Localizer[MessageKeys.Role.Created], StatusCodes.Status201Created, HttpContext.TraceIdentifier);
        return CreatedAtAction(nameof(GetById), new { id = role.Id, version = "1.0" }, body);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("ProviderRoles.Update")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> Update(
        Guid id, [FromBody] UpdateProviderRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await Mediator.Send(
            new UpdateProviderRoleCommand(id, request.Name, request.Permissions), cancellationToken);
        return OkResponse(role, MessageKeys.Role.Updated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("ProviderRoles.Delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteProviderRoleCommand(id), cancellationToken);
        return EmptyResponse(MessageKeys.Role.Deleted);
    }
}

public sealed record UpdateProviderRoleRequest(string Name, IReadOnlyList<string> Permissions);
