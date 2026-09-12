using Microsoft.AspNetCore.Authorization;

namespace Enterprise.Api.Authorization;

/// <summary>
/// The one attribute controllers actually use. Under the hood it's just
/// <c>[Authorize(Policy = "Permission:Products.Create")]</c> - this attribute exists purely so
/// call sites read as intent ("requires this permission") instead of a magic string prefix
/// convention leaking into every controller.
/// </summary>
public sealed class RequirePermissionAttribute(string permission)
    : AuthorizeAttribute(PermissionPolicyProvider.PermissionPolicyPrefix + permission);
