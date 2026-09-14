using Enterprise.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Enterprise.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string name, UserType roleType = UserType.Admin, Guid? providerId = null) : base(name)
    {
        RoleType = roleType;
        ProviderId = providerId;
    }

    public bool IsSystem { get; set; }
    public UserType RoleType { get; set; } = UserType.Admin;
    public Guid? ProviderId { get; set; }
}
