using Asp.Versioning;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.Category;
using Enterprise.Application.Features.Client.Category.Queries.GetAllClientCategoryQuery;
using Enterprise.Application.Features.Client.Category.Queries.GetClientCategoryByIdQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/{providerId:guid}/categories")]
[AllowAnonymous]
public sealed class ClientProviderCategoriesController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClientCategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<ClientCategoryDto>>>> GetList(
        [FromRoute] Guid providerId,
        [FromQuery] string? searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var request = new GetAllClientCategoryQuery
        {
            ProviderId = providerId,
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        return OkResponse(
            await Mediator.Send(request, cancellationToken),
            MessageKeys.Category.ListRetrieved);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClientCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClientCategoryDto>>> GetById(
        [FromRoute] Guid providerId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new GetClientCategoryByIdQuery(providerId, id), cancellationToken),
            MessageKeys.Category.Retrieved);
}
