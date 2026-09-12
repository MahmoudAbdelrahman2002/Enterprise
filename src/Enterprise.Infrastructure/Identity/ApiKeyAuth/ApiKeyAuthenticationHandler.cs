using System.Security.Claims;
using System.Text.Encodings.Web;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Infrastructure.Identity.ApiKeyAuth;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IUserAccountService userAccountService)
    : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var providedKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return AuthenticateResult.Fail("Empty API key.");
        }

        var keyHash = tokenService.HashToken(providedKey);
        var apiKey = await unitOfWork.ApiKeys.GetByKeyHashAsync(keyHash, Context.RequestAborted);

        if (apiKey is null || !apiKey.IsUsable)
        {
            return AuthenticateResult.Fail("Invalid, expired or revoked API key.");
        }

        var user = await userAccountService.FindByIdAsync(apiKey.UserId, Context.RequestAborted);
        if (user is null || !user.IsActive)
        {
            return AuthenticateResult.Fail("Invalid, expired or revoked API key.");
        }

        apiKey.RecordUsage();
        unitOfWork.ApiKeys.Update(apiKey);
        await unitOfWork.SaveChangesAsync(Context.RequestAborted);

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("user_type", user.UserType.ToString()),
            .. user.Roles.Select(role => new Claim(ClaimTypes.Role, role)),
            .. user.Permissions.Select(permission => new Claim("permission", permission))
        ];

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
