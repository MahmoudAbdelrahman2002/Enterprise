using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Store;
using Enterprise.Application.Features.Provider.Store.Commands.UpdateProviderStore;
using Enterprise.Application.Features.Provider.Store.Queries.GetProviderStore;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/store")]
[RequireProvider]
public sealed class ProviderStoreController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.ProviderStore.Read)]
    [ProducesResponseType(typeof(ApiResponse<ProviderStoreDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderStoreDto>>> Get(CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetProviderStoreQuery(), cancellationToken), MessageKeys.Provider.Retrieved);

    [HttpPut]
    [RequirePermission(Permissions.ProviderStore.Update)]
    [ProducesResponseType(typeof(ApiResponse<ProviderStoreDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderStoreDto>>> Update(
        [FromBody] UpdateProviderStoreRequest request,
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(
                new UpdateProviderStoreCommand(request.CompanyName, request.PhoneNumber),
                cancellationToken),
            MessageKeys.Provider.Updated);
}

public sealed record UpdateProviderStoreRequest(string CompanyName, string? PhoneNumber);
