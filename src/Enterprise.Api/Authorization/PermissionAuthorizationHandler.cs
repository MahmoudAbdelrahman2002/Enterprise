using Microsoft.AspNetCore.Authorization;

namespace Enterprise.Api.Authorization;

/// <summary>
/// Deliberately checks a "permission" claim, NOT role membership - the JWT/API-key issuers
/// (Infrastructure) flatten a user's roles into their effective permissions at token-issuance
/// time, so this handler (and every <c>[RequirePermission]</c> attribute) never needs to know
/// which roles exist or which role grants what. Reassigning a permission between roles in the
/// database changes what a user can do on their NEXT login, with zero code changes here.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
