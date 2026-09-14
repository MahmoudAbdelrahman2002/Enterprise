using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Extensions;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Auth.Commands.RefreshToken;
using Enterprise.Application.Features.Auth.Commands.RevokeToken;
using Enterprise.Application.Features.Provider.Commands.ProviderChangePassword;
using Enterprise.Application.Features.Provider.Commands.ProviderForgotPassword;
using Enterprise.Application.Features.Provider.Commands.ProviderLogin;
using Enterprise.Application.Features.Provider.Commands.ProviderResetPassword;
using Enterprise.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Enterprise.Api.Controllers.V1.Provider;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provider/auth")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public sealed class ProviderAuthController : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] ProviderLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new ProviderLoginCommand(request.Email, request.Password, ClientIpAddress), cancellationToken);
        return OkResponse(result, MessageKeys.Auth.LoginSuccess);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object?>>> ForgotPassword(
        [FromBody] ProviderForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ProviderForgotPasswordCommand(request.Email), cancellationToken);
        return EmptyResponse(MessageKeys.Auth.ForgotPasswordSent);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> ResetPassword(
        [FromBody] ProviderResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new ProviderResetPasswordCommand(request.Email, request.Otp, request.NewPassword), cancellationToken);
        return EmptyResponse(MessageKeys.Auth.PasswordResetSuccess);
    }

    [HttpPost("change-password")]
    [RequireProvider]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object?>>> ChangePassword(
        [FromBody] ProviderChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new ProviderChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
        return EmptyResponse(MessageKeys.Auth.PasswordChanged);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh(
        [FromBody] ProviderRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new RefreshTokenCommand(request.RefreshToken, ClientIpAddress, UserType.Provider), cancellationToken);
        return OkResponse(result, MessageKeys.Auth.TokenRefreshed);
    }

    [HttpPost("revoke-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Revoke(
        [FromBody] ProviderRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RevokeTokenCommand(request.RefreshToken, ClientIpAddress), cancellationToken);
        return EmptyResponse(MessageKeys.Auth.TokenRevoked);
    }
}

public sealed record ProviderLoginRequest(string Email, string Password);
public sealed record ProviderForgotPasswordRequest(string Email);
public sealed record ProviderResetPasswordRequest(string Email, string Otp, string NewPassword);
public sealed record ProviderChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ProviderRefreshTokenRequest(string RefreshToken);
