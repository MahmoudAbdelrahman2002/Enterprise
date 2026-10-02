using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Admin.Orders.Queries.GetAdminOrderById;
using Enterprise.Application.Features.Admin.Orders.Queries.GetAdminOrders;
using Enterprise.Application.Features.Orders.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/orders")]
[RequireAdmin]
public sealed class AdminOrdersController : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.Orders.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderListItemDto>>>> GetList(
        [FromQuery] GetAdminOrdersQuery query,
        CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.Order.ListRetrieved);

    [HttpGet("{orderId:guid}")]
    [RequirePermission(Permissions.Orders.Read)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetById(
        Guid orderId,
        CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new GetAdminOrderByIdQuery(orderId), cancellationToken),
            MessageKeys.Order.Retrieved);
}
