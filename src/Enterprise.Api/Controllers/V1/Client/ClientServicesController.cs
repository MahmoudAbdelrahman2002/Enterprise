using Asp.Versioning;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.Services;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Client.Providers.Queries.GetClientProvidersByService;
using Enterprise.Application.Features.Client.Services.Queries.GetAllServicesQuery;
using Enterprise.Application.Features.Client.Services.Queries.GetClientServiceByIdQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static System.Threading.CancellationToken;    

namespace Enterprise.Api.Controllers.V1.Client;

[ApiController]

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/services")]
[AllowAnonymous]
public class ClientServicesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ClientMarketServiceDto>>>> GetAll([FromQuery] GetAllServicesQuery query, CancellationToken cancellationToken)
    {
        return OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.Services.ListRetrieved);
    }
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClientMarketServiceDto>>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return OkResponse(await Mediator.Send(new GetClientServiceByIdQuery(id), cancellationToken), MessageKeys.Services.Retrieved);
    }

    [HttpGet("{id:guid}/providers")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClientProviderListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<ClientProviderListItemDto>>>> GetProviders(
        [FromRoute] Guid id,
        [FromQuery] string? searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetClientProvidersByServiceQuery
        {
            ServiceId = id,
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        return OkResponse(
            await Mediator.Send(query, cancellationToken),
            MessageKeys.Provider.ListRetrieved);
    }
}
