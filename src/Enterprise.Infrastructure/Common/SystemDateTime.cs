using Enterprise.Application.Common.Interfaces;

namespace Enterprise.Infrastructure.Common;

public sealed class SystemDateTime : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
