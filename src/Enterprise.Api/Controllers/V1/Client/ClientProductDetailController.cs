using Asp.Versioning;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.product;
using Enterprise.Application.Features.Client.product.Queries.GetClientProductById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/products")]
[AllowAnonymous]
public sealed class ClientProductDetailController : ApiControllerBase
{
    [HttpGet("{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClientProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClientProductDto>>> GetById(
        [FromRoute] Guid productId,
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new GetClientProductByIdQuery(productId), cancellationToken),
            MessageKeys.Product.Retrieved);
}
