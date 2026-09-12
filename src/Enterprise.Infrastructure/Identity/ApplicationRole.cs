using Microsoft.AspNetCore.Identity;

namespace Enterprise.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string name) : base(name)
    {
    }

    public bool IsSystem { get; set; }
}
