using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Staff.Commands.CreateProviderStaff;
using Enterprise.Application.Features.Provider.Staff.Commands.DeleteProviderStaff;
using Enterprise.Application.Features.Provider.Staff.Commands.SetProviderStaffActive;
using Enterprise.Application.Features.Provider.Staff.Commands.UpdateProviderStaff;
using Enterprise.Application.Features.Provider.Staff.Queries.GetProviderStaffById;
using Enterprise.Application.Features.Provider.Staff.Queries.GetProviderStaffList;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/staff")]
[RequireProvider]
public sealed class ProviderStaffController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission("ProviderStaff.Read")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StaffListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<StaffListItemDto>>>> GetList(
        [FromQuery] GetProviderStaffListQuery query, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.ProviderStaff.ListRetrieved);

    [HttpGet("{id:guid}")]
    [RequirePermission("ProviderStaff.Read")]
    [ProducesResponseType(typeof(ApiResponse<StaffDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<StaffDetailDto>>> GetById(
        Guid id, CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetProviderStaffByIdQuery(id), cancellationToken), MessageKeys.ProviderStaff.Retrieved);

    [HttpPost]
    [RequirePermission("ProviderStaff.Create")]
    [ProducesResponseType(typeof(ApiResponse<StaffDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<StaffDetailDto>>> Create(
        [FromBody] CreateProviderStaffRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProviderStaffCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.RoleId);

        var staff = await Mediator.Send(command, cancellationToken);
        var body = ApiResponse<StaffDetailDto>.Ok(
            staff, Localizer[MessageKeys.ProviderStaff.Created], StatusCodes.Status201Created, HttpContext.TraceIdentifier);
        return CreatedAtAction(nameof(GetById), new { id = staff.Id, version = "1.0" }, body);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("ProviderStaff.Update")]
    [ProducesResponseType(typeof(ApiResponse<StaffDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<StaffDetailDto>>> Update(
        Guid id, [FromBody] UpdateProviderStaffRequest request, CancellationToken cancellationToken)
    {
        var staff = await Mediator.Send(
            new UpdateProviderStaffCommand(id, request.FirstName, request.LastName, request.PhoneNumber, request.RoleId),
            cancellationToken);
        return OkResponse(staff, MessageKeys.ProviderStaff.Updated);
    }

    [HttpPost("{id:guid}/set-active")]
    [RequirePermission("ProviderStaff.Update")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> SetActive(
        Guid id, [FromBody] SetProviderStaffActiveRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new SetProviderStaffActiveCommand(id, request.IsActive), cancellationToken);
        return EmptyResponse(request.IsActive ? MessageKeys.ProviderStaff.Activated : MessageKeys.ProviderStaff.Deactivated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("ProviderStaff.Delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteProviderStaffCommand(id), cancellationToken);
        return EmptyResponse(MessageKeys.ProviderStaff.Deleted);
    }
}

public sealed record CreateProviderStaffRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    Guid RoleId);

public sealed record UpdateProviderStaffRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    Guid RoleId);

public sealed record SetProviderStaffActiveRequest(bool IsActive);
