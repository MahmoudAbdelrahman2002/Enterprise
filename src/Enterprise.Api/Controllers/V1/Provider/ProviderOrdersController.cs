using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Application.Features.Provider.Orders.Commands.UpdateProviderOrderStatus;
using Enterprise.Application.Features.Provider.Orders.Queries.GetProviderOrderById;
using Enterprise.Application.Features.Provider.Orders.Queries.GetProviderOrders;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/orders")]
[RequireProvider]
public sealed class ProviderOrdersController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.ProviderOrder.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderListItemDto>>>> GetList(
        [FromQuery] GetProviderOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return OkResponse(result, MessageKeys.Order.ListRetrieved);
    }

    [HttpGet("{orderId:guid}")]
    [RequirePermission(Permissions.ProviderOrder.Read)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetById(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetProviderOrderByIdQuery(orderId), cancellationToken);
        return OkResponse(result, MessageKeys.Order.Retrieved);
    }

    [HttpPatch("{orderId:guid}/status")]
    [RequirePermission(Permissions.ProviderOrder.Update)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> UpdateStatus(
        [FromRoute] Guid orderId,
        [FromBody] UpdateOrderStatusDto body,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(
            new UpdateProviderOrderStatusCommand(orderId, body.Status),
            cancellationToken);
        return OkResponse(result, MessageKeys.Order.Updated);
    }
}
