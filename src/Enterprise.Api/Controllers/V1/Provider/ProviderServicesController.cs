using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Application.Features.Admin.Services.Queries.GetServicesLookup;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/services")]
[RequireProvider]
public sealed class ProviderServicesController : ApiControllerBase
{
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MarketplaceServiceLookupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MarketplaceServiceLookupDto>>>> GetLookup(
        CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetServicesLookupQuery(), cancellationToken), MessageKeys.Service.ListRetrieved);
}
