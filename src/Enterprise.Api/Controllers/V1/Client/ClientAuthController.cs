using Asp.Versioning;
using Enterprise.Api.Controllers;
using Enterprise.Api.Extensions;
using Enterprise.Api.Models;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Auth.Commands.RefreshToken;
using Enterprise.Application.Features.Auth.Commands.RevokeToken;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.Auth;
using Enterprise.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/auth")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public sealed class ClientAuthController : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<OtpSentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<OtpSentDto>>> Register(
        [FromBody] ClientRegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new ClientRegisterCommand(request.Email, request.FirstName, request.LastName), cancellationToken);
        return OkResponseText(result, result.Message);
    }

    [HttpPost("verify-registration")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> VerifyRegistration(
        [FromBody] ClientOtpVerifyRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new ClientVerifyRegistrationCommand(request.Email, request.Otp, ClientIpAddress), cancellationToken);
        return OkResponse(result, MessageKeys.Auth.RegistrationVerified);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<OtpSentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OtpSentDto>>> Login(
        [FromBody] ClientLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ClientLoginCommand(request.Email), cancellationToken);
        return OkResponseText(result, result.Message);
    }

    [HttpPost("verify-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> VerifyLogin(
        [FromBody] ClientOtpVerifyRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new ClientVerifyLoginCommand(request.Email, request.Otp, ClientIpAddress), cancellationToken);
        return OkResponse(result, MessageKeys.Auth.LoginSuccess);
    }

    [HttpPost("external")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> ExternalLogin(
        [FromBody] ClientExternalLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new ClientExternalLoginCommand(request.Provider, request.IdToken, ClientIpAddress),
            cancellationToken);
        return OkResponse(result, MessageKeys.Auth.ExternalLoginSuccess);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh(
        [FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new RefreshTokenCommand(request.RefreshToken, ClientIpAddress, UserType.Client), cancellationToken);
        return OkResponse(result, MessageKeys.Auth.TokenRefreshed);
    }

    [HttpPost("revoke-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Revoke(
        [FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RevokeTokenCommand(request.RefreshToken, ClientIpAddress), cancellationToken);
        return EmptyResponse(MessageKeys.Auth.TokenRevoked);
    }
}

public sealed record ClientRegisterRequest(string Email, string FirstName, string LastName);
public sealed record ClientLoginRequest(string Email);
public sealed record ClientOtpVerifyRequest(string Email, string Otp);
public sealed record ClientExternalLoginRequest(string Provider, string IdToken);
public sealed record RefreshTokenRequest(string RefreshToken);
