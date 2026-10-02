using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

public class DeviceToken : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = "";
    public string? Platform { get; set; }

    public DeviceToken()
    {
    }

    public DeviceToken(Guid userId, string token, string? platform = null)
    {
        UserId = userId;
        Token = token;
        Platform = platform;
    }

    public void Update(Guid userId, string? platform)
    {
        UserId = userId;
        Platform = platform;
    }
}
