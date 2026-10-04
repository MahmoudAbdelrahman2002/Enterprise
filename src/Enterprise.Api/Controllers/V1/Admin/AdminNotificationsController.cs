using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Notifications.Queries.GetNotificationsPage;
using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Notifications;
using Enterprise.Application.Features.Notifications.Commands.MarkNotificationRead;
using Enterprise.Application.Features.Notifications.Commands.RegisterDeviceToken;
using Enterprise.Application.Features.Notifications.Commands.UnregisterDeviceToken;
using Enterprise.Application.Features.Notifications.Queries.GetNotifications;
using Enterprise.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[RequireAdmin]
public sealed class AdminNotificationsController : ApiControllerBase
{
    [HttpGet("notifications/paged")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetPage([FromQuery] GetNotificationsPageQuery query, CancellationToken cancellationToken = default) =>
        OkResponse(await Mediator.Send(query, cancellationToken), MessageKeys.Notification.ListRetrieved);

    [HttpGet("notifications")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetNotificationsQuery(), cancellationToken);
        return OkResponse(result, MessageKeys.Notification.ListRetrieved);
    }

    [HttpGet("notifications/count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadNotificationCountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UnreadNotificationCountDto>>> GetCount(
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetUnreadNotificationCountQuery(), cancellationToken);
        return OkResponse(result, MessageKeys.Notification.CountRetrieved);
    }

    [HttpPost("notifications/{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> MarkAsRead(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return EmptyResponse(MessageKeys.Notification.MarkedAsRead);
    }

    [HttpPost("device-tokens")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object?>>> RegisterDeviceToken(
        [FromBody] DeviceTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new RegisterDeviceTokenCommand(request.Token, request.Platform), cancellationToken);
        return EmptyResponse(MessageKeys.Notification.DeviceTokenRegistered);
    }

    [HttpDelete("device-tokens")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object?>>> UnregisterDeviceToken(
        [FromBody] DeviceTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new UnregisterDeviceTokenCommand(request.Token), cancellationToken);
        return EmptyResponse(MessageKeys.Notification.DeviceTokenUnregistered);
    }
}
