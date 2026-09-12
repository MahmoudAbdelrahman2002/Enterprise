using Enterprise.Api.Models;
using Enterprise.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers;

/// <summary>
/// Every controller in this template is intentionally thin: no validation, no business logic,
/// no direct repository/DbContext access - just "bind the request, send it through MediatR,
/// map the result to an HTTP status code". <see cref="Mediator"/> is resolved lazily from
/// <see cref="ControllerBase.HttpContext"/> rather than via constructor injection purely to
/// avoid repeating an identical one-line constructor in every single controller.
/// </summary>
[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    private IAppLocalizer? _localizer;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IAppLocalizer Localizer =>
        _localizer ??= HttpContext.RequestServices.GetRequiredService<IAppLocalizer>();

    protected string? ClientIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();

    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string messageKey) =>
        StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<T>.Ok(data, Localizer[messageKey], StatusCodes.Status200OK, HttpContext.TraceIdentifier));

    protected ActionResult<ApiResponse<T>> OkResponseText<T>(T data, string localizedMessage) =>
        StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<T>.Ok(data, localizedMessage, StatusCodes.Status200OK, HttpContext.TraceIdentifier));

    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(T data, string messageKey) =>
        StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<T>.Ok(data, Localizer[messageKey], StatusCodes.Status201Created, HttpContext.TraceIdentifier));

    protected ActionResult<ApiResponse<object?>> EmptyResponse(string messageKey) =>
        StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<object?>.Ok(null, Localizer[messageKey], StatusCodes.Status200OK, HttpContext.TraceIdentifier));
}
