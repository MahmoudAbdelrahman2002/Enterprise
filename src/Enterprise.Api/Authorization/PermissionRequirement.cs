using Microsoft.AspNetCore.Authorization;

namespace Enterprise.Api.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
