using System.Security.Claims;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using Enterprise.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Identity;

public sealed class RoleManagerService(
    RoleManager<ApplicationRole> roleManager,
    ApplicationDbContext context) : IRoleManagerService
{
    public async Task<PagedResult<RoleListItemDto>> GetRolesAsync(
        UserType portal,
        Guid? providerId,
        PaginationParams pagination,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Roles.AsNoTracking().Where(r => r.RoleType == portal);

        if (portal == UserType.Provider)
        {
            query = query.Where(r => r.ProviderId == providerId);
        }
        else
        {
            query = query.Where(r => r.ProviderId == null);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var trimmedSearch = searchTerm.Trim();
            query = query.Where(r => r.Name != null && r.Name.Contains(trimmedSearch));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var roles = await query
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(r => r.Id).ToList();

        var claimsByRole = await context.RoleClaims
            .AsNoTracking()
            .Where(rc => roleIds.Contains(rc.RoleId) && rc.ClaimType == "permission" && rc.ClaimValue != null)
            .GroupBy(rc => rc.RoleId)
            .ToDictionaryAsync(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(rc => rc.ClaimValue!).ToList(),
                cancellationToken);

        var userCountsByRole = await context.UserRoles
            .AsNoTracking()
            .Where(ur => roleIds.Contains(ur.RoleId))
            .GroupBy(ur => ur.RoleId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);

        var items = roles.Select(role => new RoleListItemDto(
            role.Id,
            role.Name ?? string.Empty,
            role.RoleType,
            role.ProviderId,
            role.IsSystem,
            userCountsByRole.GetValueOrDefault(role.Id, 0),
            claimsByRole.GetValueOrDefault(role.Id, Array.Empty<string>())
        )).ToList();

        return new PagedResult<RoleListItemDto>(items, totalItems, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<RoleDetailDto?> GetRoleByIdAsync(
        Guid roleId,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Roles.AsNoTracking().Where(r => r.Id == roleId && r.RoleType == portal);

        if (portal == UserType.Provider)
        {
            query = query.Where(r => r.ProviderId == providerId);
        }
        else
        {
            query = query.Where(r => r.ProviderId == null);
        }

        var role = await query.FirstOrDefaultAsync(cancellationToken);
        if (role is null)
        {
            return null;
        }

        var permissions = await context.RoleClaims
            .AsNoTracking()
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == "permission" && rc.ClaimValue != null)
            .Select(rc => rc.ClaimValue!)
            .ToListAsync(cancellationToken);

        var userCount = await context.UserRoles
            .AsNoTracking()
            .CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

        return new RoleDetailDto(
            role.Id,
            role.Name ?? string.Empty,
            role.RoleType,
            role.ProviderId,
            role.IsSystem,
            userCount,
            permissions
        );
    }

    public async Task<CreateRoleResult> CreateRoleAsync(
        string name,
        UserType portal,
        Guid? providerId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var distinctPermissions = permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var perm in distinctPermissions)
        {
            if (!PermissionCatalog.IsValidForPortal(portal, perm))
            {
                return new CreateRoleResult(false, Error: MessageKeys.Role.InvalidPermissions);
            }
        }

        var trimmedName = name.Trim();
        var nameQuery = context.Roles.Where(r => r.RoleType == portal && r.Name == trimmedName);
        if (portal == UserType.Provider)
        {
            nameQuery = nameQuery.Where(r => r.ProviderId == providerId);
        }
        else
        {
            nameQuery = nameQuery.Where(r => r.ProviderId == null);
        }

        if (await nameQuery.AnyAsync(cancellationToken))
        {
            return new CreateRoleResult(false, Error: MessageKeys.Role.NameExists);
        }

        var role = new ApplicationRole(trimmedName, portal, providerId)
        {
            Id = Guid.NewGuid(),
            IsSystem = false
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return new CreateRoleResult(false, Error: result.Errors.FirstOrDefault()?.Description);
        }
        
        foreach (var permission in distinctPermissions)
        {
            await roleManager.AddClaimAsync(role, new Claim("permission", permission));
        }

        return new CreateRoleResult(true, RoleId: role.Id);
    }

    public async Task<UpdateRoleResult> UpdateRoleAsync(
        Guid roleId,
        string name,
        UserType portal,
        Guid? providerId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || role.RoleType != portal || (portal == UserType.Provider && role.ProviderId != providerId) || (portal == UserType.Admin && role.ProviderId != null))
        {
            return new UpdateRoleResult(false, Error: MessageKeys.Error.NotFound);
        }

        if (role.IsSystem)
        {
            return new UpdateRoleResult(false, Error: MessageKeys.Role.CannotModifySystemRole);
        }

        var distinctPermissions = permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var perm in distinctPermissions)
        {
            if (!PermissionCatalog.IsValidForPortal(portal, perm))
            {
                return new UpdateRoleResult(false, Error: MessageKeys.Role.InvalidPermissions);
            }
        }

        var trimmedName = name.Trim();
        if (!string.Equals(role.Name, trimmedName, StringComparison.OrdinalIgnoreCase))
        {
            var nameQuery = context.Roles.Where(r => r.Id != roleId && r.RoleType == portal && r.Name == trimmedName);
            if (portal == UserType.Provider)
            {
                nameQuery = nameQuery.Where(r => r.ProviderId == providerId);
            }
            else
            {
                nameQuery = nameQuery.Where(r => r.ProviderId == null);
            }

            if (await nameQuery.AnyAsync(cancellationToken))
            {
                return new UpdateRoleResult(false, Error: MessageKeys.Role.NameExists);
            }

            role.Name = trimmedName;
            role.NormalizedName = trimmedName.ToUpperInvariant();
            var updateResult = await roleManager.UpdateAsync(role);
            if (!updateResult.Succeeded)
            {
                return new UpdateRoleResult(false, Error: updateResult.Errors.FirstOrDefault()?.Description);
            }
        }

        var existingClaims = await roleManager.GetClaimsAsync(role);
        var existingPermissions = existingClaims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();

        // Remove stale claims
        foreach (var claim in existingClaims.Where(c => c.Type == "permission"))
        {
            if (!distinctPermissions.Contains(claim.Value, StringComparer.OrdinalIgnoreCase))
            {
                await roleManager.RemoveClaimAsync(role, claim);
            }
        }

        // Add missing claims
        foreach (var permission in distinctPermissions)
        {
            if (!existingPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            {
                await roleManager.AddClaimAsync(role, new Claim("permission", permission));
            }
        }

        return new UpdateRoleResult(true);
    }

    public async Task<DeleteRoleResult> DeleteRoleAsync(
        Guid roleId,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || role.RoleType != portal || (portal == UserType.Provider && role.ProviderId != providerId) || (portal == UserType.Admin && role.ProviderId != null))
        {
            return new DeleteRoleResult(false, Error: MessageKeys.Error.NotFound);
        }

        if (role.IsSystem)
        {
            return new DeleteRoleResult(false, Error: MessageKeys.Role.CannotDeleteSystemRole);
        }

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return new DeleteRoleResult(false, Error: result.Errors.FirstOrDefault()?.Description);
        }

        return new DeleteRoleResult(true);
    }
}
