using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;

namespace Enterprise.Application.Common.Interfaces;

public sealed record StaffListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    Guid RoleId,
    string RoleName,
    UserType UserType,
    Guid? ProviderId,
    bool IsActive,
    bool IsSystem);

public sealed record StaffDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    Guid RoleId,
    string RoleName,
    IReadOnlyList<string> Permissions,
    UserType UserType,
    Guid? ProviderId,
    bool IsActive,
    bool IsSystem);

public sealed record CreateStaffRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    Guid RoleId);

public sealed record UpdateStaffRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    Guid RoleId);

public sealed record CreateStaffResult(bool Succeeded, Guid? StaffId = null, string? Error = null, IReadOnlyList<string>? Errors = null);

public sealed record UpdateStaffResult(bool Succeeded, string? Error = null, IReadOnlyList<string>? Errors = null);

public sealed record SetActiveResult(bool Succeeded, string? Error = null, IReadOnlyList<string>? Errors = null);

public sealed record DeleteStaffResult(bool Succeeded, string? Error = null, IReadOnlyList<string>? Errors = null);

public interface IStaffManagerService
{
    Task<PagedResult<StaffListItemDto>> GetStaffListAsync(
        UserType portal,
        Guid? providerId,
        PaginationParams pagination,
        string? searchTerm = null,
        Guid? roleId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<StaffDetailDto?> GetStaffByIdAsync(
        Guid staffId,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default);

    Task<CreateStaffResult> CreateStaffAsync(
        CreateStaffRequest request,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default);

    Task<UpdateStaffResult> UpdateStaffAsync(
        Guid staffId,
        UpdateStaffRequest request,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default);

    Task<SetActiveResult> SetStaffActiveAsync(
        Guid staffId,
        bool isActive,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default);

    Task<DeleteStaffResult> DeleteStaffAsync(
        Guid staffId,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default);
}
