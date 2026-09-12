using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Profiles;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/profile")]
[RequireClient]
public sealed class ClientProfileController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> Get(CancellationToken cancellationToken) =>
        OkResponse(await Mediator.Send(new GetProfileQuery(), cancellationToken), MessageKeys.Profile.Retrieved);

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> Update(
        [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new UpdateProfileCommand(request.FirstName, request.LastName), cancellationToken),
            MessageKeys.Profile.Updated);

    [HttpPost("change-email/request")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> RequestChangeEmail(
        [FromBody] ChangeEmailRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RequestChangeEmailCommand(request.NewEmail), cancellationToken);
        return EmptyResponse(MessageKeys.Profile.EmailChangeCodeSent);
    }

    [HttpPost("change-email/confirm")]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> ConfirmChangeEmail(
        [FromBody] ConfirmChangeEmailRequest request, CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new ConfirmChangeEmailCommand(request.NewEmail, request.Otp), cancellationToken),
            MessageKeys.Profile.EmailChanged);
}

public sealed record UpdateProfileRequest(string FirstName, string LastName);
public sealed record ChangeEmailRequest(string NewEmail);
public sealed record ConfirmChangeEmailRequest(string NewEmail, string Otp);
