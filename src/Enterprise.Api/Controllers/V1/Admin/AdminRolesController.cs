using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Admin.Roles.Commands.CreateAdminRole;
using Enterprise.Application.Features.Admin.Roles.Commands.DeleteAdminRole;
using Enterprise.Application.Features.Admin.Roles.Commands.UpdateAdminRole;
using Enterprise.Application.Features.Admin.Roles.Queries.GetAdminRoleById;
using Enterprise.Application.Features.Admin.Roles.Queries.GetAdminRolesList;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/roles")]
[RequireAdmin]
public sealed class AdminRolesController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission("Roles.Read")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RoleListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<RoleListItemDto>>>> GetList(
        [FromQuery] GetAdminRolesListQuery query, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.Role.ListRetrieved);

    [HttpGet("{id:guid}")]
    [RequirePermission("Roles.Read")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> GetById(
        Guid id, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetAdminRoleByIdQuery(id), cancellationToken), MessageKeys.Role.Retrieved);

    [HttpPost]
    [RequirePermission("Roles.Create")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> Create(
        [FromBody] CreateAdminRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await Mediator.Send(command, cancellationToken);
        var body = ApiResponse<RoleDetailDto>.Ok(
            role, Localizer[MessageKeys.Role.Created], StatusCodes.Status201Created, HttpContext.TraceIdentifier);
        return CreatedAtAction(nameof(GetById), new { id = role.Id, version = "1.0" }, body);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Roles.Update")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> Update(
        Guid id, [FromBody] UpdateAdminRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await Mediator.Send(
            new UpdateAdminRoleCommand(id, request.Name, request.Permissions), cancellationToken);
        return OkResponse(role, MessageKeys.Role.Updated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Roles.Delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteAdminRoleCommand(id), cancellationToken);
        return EmptyResponse(MessageKeys.Role.Deleted);
    }
}

public sealed record UpdateAdminRoleRequest(string Name, IReadOnlyList<string> Permissions);
