using Microsoft.AspNetCore.Authorization;

namespace Enterprise.Api.Authorization;

public sealed class RequireClientAttribute() : AuthorizeAttribute("RequireClient");

public sealed class RequireAdminAttribute() : AuthorizeAttribute("RequireAdmin");
