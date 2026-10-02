using Asp.Versioning;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.Providers.Queries.GetClientProviderById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/providers")]
[AllowAnonymous]
public sealed class ClientProvidersController : ApiControllerBase
{
    [HttpGet("{providerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClientProviderListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClientProviderListItemDto>>> GetById(
        [FromRoute] Guid providerId,
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new GetClientProviderByIdQuery(providerId), cancellationToken),
            MessageKeys.Provider.Retrieved);
}
