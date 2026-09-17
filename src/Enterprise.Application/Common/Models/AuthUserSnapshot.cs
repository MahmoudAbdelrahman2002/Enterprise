using Enterprise.Domain.Enums;

namespace Enterprise.Application.Common.Models;

public sealed record AuthUserSnapshot(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserType UserType,
    bool EmailConfirmed,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool IsSystem = false,
    Guid? ProviderId = null);
