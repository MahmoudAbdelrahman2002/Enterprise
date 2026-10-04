using Asp.Versioning;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.Orders.Queries.GetClientOrdersPage;
using Enterprise.Api.Authorization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.Orders.Queries.GetClientOrderById;
using Enterprise.Application.Features.Client.Orders.Queries.GetClientOrderBySession;
using Enterprise.Application.Features.Client.Orders.Queries.GetClientOrders;
using Enterprise.Application.Features.Client.Orders.Queries.GetClientOrdersByProvider;
using Enterprise.Application.Features.Client.Payments.Commands.ConfirmClientCheckout;
using Enterprise.Application.Features.Orders.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client")]
[RequireClient]
public sealed class ClientOrdersController : ApiControllerBase
{
    [HttpGet("orders/paged")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderListItemDto>>>> GetPage([FromQuery] GetClientOrdersPageQuery query, CancellationToken cancellationToken = default) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.Order.ListRetrieved);

    [HttpGet("orders")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderListItemDto>>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetClientOrdersQuery(), cancellationToken);
        return OkResponse(result, MessageKeys.Order.ListRetrieved);
    }

    [HttpGet("{providerId:guid}/orders")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderListItemDto>>>> GetByProvider(
        [FromRoute] Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetClientOrdersByProviderQuery(providerId), cancellationToken);
        return OkResponse(result, MessageKeys.Order.ListRetrieved);
    }

    [HttpGet("orders/by-session/{sessionId}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetBySession(
        [FromRoute] string sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetClientOrderBySessionQuery(sessionId), cancellationToken);
        return OkResponse(result, MessageKeys.Order.Retrieved);
    }

    /// <summary>
    /// Confirms a paid Stripe Checkout session and creates the order if the webhook has not yet arrived.
    /// </summary>
    [HttpPost("orders/confirm-session/{sessionId}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> ConfirmSession(
        [FromRoute] string sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new ConfirmClientCheckoutCommand(sessionId), cancellationToken);
        return OkResponse(result, MessageKeys.Order.Retrieved);
    }

    [HttpGet("orders/{orderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetById(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetClientOrderByIdQuery(orderId), cancellationToken);
        return OkResponse(result, MessageKeys.Order.Retrieved);
    }
}
