using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Admin.Users.Commands.CreateAdminUser;
using Enterprise.Application.Features.Admin.Users.Commands.DeleteAdminUser;
using Enterprise.Application.Features.Admin.Users.Commands.SetAdminUserActive;
using Enterprise.Application.Features.Admin.Users.Commands.UpdateAdminUser;
using Enterprise.Application.Features.Admin.Users.Queries.GetAdminUserById;
using Enterprise.Application.Features.Admin.Users.Queries.GetAdminUsersList;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/users")]
[RequireAdmin]
public sealed class AdminUsersController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission("Admins.Read")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StaffListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<StaffListItemDto>>>> GetList(
        [FromQuery] GetAdminUsersListQuery query, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.AdminUser.ListRetrieved);

    [HttpGet("{id:guid}")]
    [RequirePermission("Admins.Read")]
    [ProducesResponseType(typeof(ApiResponse<StaffDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<StaffDetailDto>>> GetById(
        Guid id, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetAdminUserByIdQuery(id), cancellationToken), MessageKeys.AdminUser.Retrieved);

    [HttpPost]
    [RequirePermission("Admins.Create")]
    [ProducesResponseType(typeof(ApiResponse<StaffDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<StaffDetailDto>>> Create(
        [FromBody] CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAdminUserCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.RoleId);

        var staff = await Mediator.Send(command, cancellationToken);
        var body = ApiResponse<StaffDetailDto>.Ok(
            staff, Localizer[MessageKeys.AdminUser.Created], StatusCodes.Status201Created, HttpContext.TraceIdentifier);
        return CreatedAtAction(nameof(GetById), new { id = staff.Id, version = "1.0" }, body);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Admins.Update")]
    [ProducesResponseType(typeof(ApiResponse<StaffDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<StaffDetailDto>>> Update(
        Guid id, [FromBody] UpdateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var staff = await Mediator.Send(
            new UpdateAdminUserCommand(id, request.FirstName, request.LastName, request.PhoneNumber, request.RoleId),
            cancellationToken);
        return OkResponse(staff, MessageKeys.AdminUser.Updated);
    }

    [HttpPost("{id:guid}/set-active")]
    [RequirePermission("Admins.Update")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> SetActive(
        Guid id, [FromBody] SetStaffActiveRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new SetAdminUserActiveCommand(id, request.IsActive), cancellationToken);
        return EmptyResponse(request.IsActive ? MessageKeys.AdminUser.Activated : MessageKeys.AdminUser.Deactivated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Admins.Delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteAdminUserCommand(id), cancellationToken);
        return EmptyResponse(MessageKeys.AdminUser.Deleted);
    }
}

public sealed record CreateAdminUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    Guid RoleId);

public sealed record UpdateAdminUserRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    Guid RoleId);

public sealed record SetStaffActiveRequest(bool IsActive);
