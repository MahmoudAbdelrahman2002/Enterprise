using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Extensions;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Profiles;
using Enterprise.Application.Features.Provider.Commands.DeleteProviderImage;
using Enterprise.Application.Features.Provider.Commands.UploadProviderImage;
using Enterprise.Application.Features.Providers;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/profile")]
[RequireProvider]
public sealed class ProviderProfileController : ApiControllerBase
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
        [FromBody] ProviderUpdateProfileRequest request, CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new UpdateProfileCommand(request.FirstName, request.LastName), cancellationToken),
            MessageKeys.Profile.Updated);

    [HttpPost("image")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> UploadImage(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new UploadProviderImageCommand(file.ToImageUploadFile()),
            cancellationToken);
        return OkResponse(result, MessageKeys.Image.Uploaded);
    }

    [HttpDelete("image")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> DeleteImage(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteProviderImageCommand(), cancellationToken);
        return OkResponse(result, MessageKeys.Image.Removed);
    }

    [HttpPost("change-email/request")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> RequestChangeEmail(
        [FromBody] ProviderChangeEmailRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RequestChangeEmailCommand(request.NewEmail), cancellationToken);
        return EmptyResponse(MessageKeys.Profile.EmailChangeCodeSent);
    }

    [HttpPost("change-email/confirm")]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> ConfirmChangeEmail(
        [FromBody] ProviderConfirmChangeEmailRequest request, CancellationToken cancellationToken) =>
        OkResponse(
            await Mediator.Send(new ConfirmChangeEmailCommand(request.NewEmail, request.Otp), cancellationToken),
            MessageKeys.Profile.EmailChanged);
}

public sealed record ProviderUpdateProfileRequest(string FirstName, string LastName);
public sealed record ProviderChangeEmailRequest(string NewEmail);
public sealed record ProviderConfirmChangeEmailRequest(string NewEmail, string Otp);
