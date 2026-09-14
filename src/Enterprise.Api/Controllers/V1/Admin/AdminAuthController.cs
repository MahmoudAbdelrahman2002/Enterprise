using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Controllers;
using Enterprise.Api.Extensions;
using Enterprise.Api.Models;
using Enterprise.Application.Features.Admin.Commands.AdminChangePassword;
using Enterprise.Application.Features.Admin.Commands.AdminForgotPassword;
using Enterprise.Application.Features.Admin.Commands.AdminLogin;
using Enterprise.Application.Features.Admin.Commands.AdminResetPassword;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Auth.Commands.RefreshToken;
using Enterprise.Application.Features.Auth.Commands.RevokeToken;
using Enterprise.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Enterprise.Api.Controllers.V1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/auth")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public sealed class AdminAuthController : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new AdminLoginCommand(request.Email, request.Password, ClientIpAddress), cancellationToken);
        return OkResponse(result, MessageKeys.Auth.LoginSuccess);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object?>>> ForgotPassword(
        [FromBody] AdminForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new AdminForgotPasswordCommand(request.Email), cancellationToken);
        return EmptyResponse(MessageKeys.Auth.ForgotPasswordSent);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> ResetPassword(
        [FromBody] AdminResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new AdminResetPasswordCommand(request.Email, request.Otp, request.NewPassword), cancellationToken);
        return EmptyResponse(MessageKeys.Auth.PasswordResetSuccess);
    }

    [HttpPost("change-password")]
    [RequireAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object?>>> ChangePassword(
        [FromBody] AdminChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new AdminChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
        return EmptyResponse(MessageKeys.Auth.PasswordChanged);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh(
        [FromBody] AdminRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new RefreshTokenCommand(request.RefreshToken, ClientIpAddress, UserType.Admin), cancellationToken);
        return OkResponse(result, MessageKeys.Auth.TokenRefreshed);
    }

    [HttpPost("revoke-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Revoke(
        [FromBody] AdminRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RevokeTokenCommand(request.RefreshToken, ClientIpAddress), cancellationToken);
        return EmptyResponse(MessageKeys.Auth.TokenRevoked);
    }
}

public sealed record AdminLoginRequest(string Email, string Password);
public sealed record AdminForgotPasswordRequest(string Email);
public sealed record AdminResetPasswordRequest(string Email, string Otp, string NewPassword);
public sealed record AdminChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record AdminRefreshTokenRequest(string RefreshToken);
