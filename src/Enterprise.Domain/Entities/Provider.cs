using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

/// <summary>
/// Marketplace seller (Subito provider) store profile, linked 1:1 to an Identity user
/// with <c>UserType.Provider</c>. Accounts are provisioned only by Admin.
/// </summary>
public sealed class Provider : BaseAuditableEntity, ISoftDelete
{
    private Provider()
    {
    }

    public Provider(Guid userId, string companyName, string? phoneNumber = null)
    {
        UserId = userId;
        CompanyName = companyName;
        PhoneNumber = phoneNumber;
    }

    public Guid UserId { get; private set; }
    public string CompanyName { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    public void UpdateDetails(string companyName, string? phoneNumber)
    {
        CompanyName = companyName;
        PhoneNumber = phoneNumber;
    }
}
