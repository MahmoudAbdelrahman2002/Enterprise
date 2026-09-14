using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Admin.Roles.Queries.GetAdminPermissions;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/permissions")]
[RequireAdmin]
public sealed class AdminPermissionsController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission("Roles.Read")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionGroupDto>>>> GetPermissions(
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new GetAdminPermissionsQuery(), cancellationToken),
            MessageKeys.Permission.ListRetrieved);
}
