using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.Cart.Queries;
using Enterprise.Application.Features.Client.Cart.Queries.GetClientCarts;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/carts")]
[RequireClient]
public sealed class ClientCartsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShoppingCartDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShoppingCartDto>>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetClientCartsQuery(), cancellationToken);
        return OkResponse(result, MessageKeys.Cart.ListRetrieved);
    }
}
