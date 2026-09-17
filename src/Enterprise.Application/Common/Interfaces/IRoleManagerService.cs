using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;

namespace Enterprise.Application.Common.Interfaces;

public sealed record RoleListItemDto(
    Guid Id,
    string Name,
    UserType RoleType,
    Guid? ProviderId,
    bool IsSystem,
    int UsersCount,
    IReadOnlyList<string> Permissions);

public sealed record RoleDetailDto(
    Guid Id,
    string Name,
    UserType RoleType,
    Guid? ProviderId,
    bool IsSystem,
    int UsersCount,
    IReadOnlyList<string> Permissions);

public sealed record CreateRoleResult(bool Succeeded, Guid? RoleId = null, string? Error = null, IReadOnlyList<string>? Errors = null);

public sealed record UpdateRoleResult(bool Succeeded, string? Error = null, IReadOnlyList<string>? Errors = null);

public sealed record DeleteRoleResult(bool Succeeded, string? Error = null, IReadOnlyList<string>? Errors = null);

public interface IRoleManagerService
{
    Task<PagedResult<RoleListItemDto>> GetRolesAsync(
        UserType portal,
        Guid? providerId,
        PaginationParams pagination,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<RoleDetailDto?> GetRoleByIdAsync(
        Guid roleId,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default);

    Task<CreateRoleResult> CreateRoleAsync(
        string name,
        UserType portal,
        Guid? providerId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);

    Task<UpdateRoleResult> UpdateRoleAsync(
        Guid roleId,
        string name,
        UserType portal,
        Guid? providerId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);

    Task<DeleteRoleResult> DeleteRoleAsync(
        Guid roleId,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default);
}
