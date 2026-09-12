using Enterprise.Infrastructure.Identity.ApiKeyAuth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Enterprise.Api.Authorization;

/// <summary>
/// Without this, every permission string used anywhere in the codebase would need a matching
/// <c>services.AddAuthorization(options => options.AddPolicy("Products.Create", ...))</c> line
/// in <c>Program.cs</c> - a second source of truth that WILL drift from the
/// <c>[RequirePermission]</c> attributes as the system grows. Instead, any policy name matching
/// the <see cref="PermissionPolicyPrefix"/> convention is synthesized on demand from the
/// permission name embedded in it; everything else falls back to the framework's default
/// provider (so ordinary named/role-based policies still work unchanged).
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    public const string PermissionPolicyPrefix = "Permission:";

    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PermissionPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        var permission = policyName[PermissionPolicyPrefix.Length..];

        // Explicitly listing both schemes (rather than relying on DefaultAuthenticateScheme)
        // means a request authenticated ONLY via API key still gets evaluated correctly here -
        // without this, a dynamically-built policy would only ever see whichever scheme the
        // authentication middleware defaults to.
        var policy = new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthenticationDefaults.SchemeName)
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
