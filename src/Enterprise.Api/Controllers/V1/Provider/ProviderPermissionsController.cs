using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Roles.Queries.GetProviderPermissions;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/permissions")]
[RequireProvider]
public sealed class ProviderPermissionsController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission("ProviderRoles.Read")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionGroupDto>>>> GetPermissions(
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new GetProviderPermissionsQuery(), cancellationToken),
            MessageKeys.Permission.ListRetrieved);
}
