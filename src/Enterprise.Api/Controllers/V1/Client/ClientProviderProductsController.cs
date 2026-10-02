using Asp.Versioning;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.product;
using Enterprise.Application.Features.Client.product.Queries.GetClientProductsByProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/{providerId:guid}/products")]
[AllowAnonymous]
public sealed class ClientProviderProductsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClientProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<ClientProductDto>>>> GetList(
        [FromRoute] Guid providerId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var request = new GetClientProductsByProviderQuery
        {
            ProviderId = providerId,
            CategoryId = categoryId,
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        return OkResponse(
            await Mediator.Send(request, cancellationToken),
            MessageKeys.Product.ListRetrieved);
    }
}
