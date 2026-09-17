using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Enterprise.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public UserType? UserType
    {
        get
        {
            var value = User?.FindFirstValue("user_type");
            return Enum.TryParse<UserType>(value, ignoreCase: true, out var type) ? type : null;
        }
    }

    public Guid? ProviderId
    {
        get
        {
            var value = User?.FindFirstValue("provider_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    public IReadOnlyCollection<string> Permissions =>
        User?.FindAll("permission").Select(c => c.Value).ToList() ?? [];
}
