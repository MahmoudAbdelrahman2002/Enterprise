using System.Security.Claims;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Identity;

public sealed class UserAccountService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : IUserAccountService
{
    public const string ClientRoleName = "Client";
    public const string AdminRoleName = "Admin";
    public const string ProviderRoleName = "Provider";

    public async Task<AuthUserSnapshot?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(
            u => u.NormalizedEmail == userManager.NormalizeEmail(email), cancellationToken);
        return user is null ? null : await ToSnapshotAsync(user);
    }

    public async Task<AuthUserSnapshot?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        return user is null ? null : await ToSnapshotAsync(user);
    }

    public async Task<AuthUserSnapshot?> FindByLoginAsync(
        string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByLoginAsync(loginProvider, providerKey);
        return user is null ? null : await ToSnapshotAsync(user);
    }

    public async Task<AccountOperationResult> CreateClientAsync(
        string email, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        return await CreateClientInternalAsync(email, firstName, lastName, emailConfirmed: false, cancellationToken);
    }

    public async Task<AccountOperationResult> CreateClientFromExternalAsync(
        string email, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        return await CreateClientInternalAsync(email, firstName, lastName, emailConfirmed: true, cancellationToken);
    }

    public async Task<AccountOperationResult> AddLoginAsync(
        Guid userId,
        string loginProvider,
        string providerKey,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        var existing = await userManager.FindByLoginAsync(loginProvider, providerKey);
        if (existing is not null)
        {
            return existing.Id == userId
                ? new AccountOperationResult(true)
                : new AccountOperationResult(false, MessageKeys.Auth.SocialAlreadyLinked);
        }

        var logins = await userManager.GetLoginsAsync(user);
        if (logins.Any(l =>
                l.LoginProvider.Equals(loginProvider, StringComparison.OrdinalIgnoreCase)
                && l.ProviderKey.Equals(providerKey, StringComparison.Ordinal)))
        {
            return new AccountOperationResult(true);
        }

        var result = await userManager.AddLoginAsync(
            user, new UserLoginInfo(loginProvider, providerKey, displayName ?? loginProvider));
        return result.Succeeded
            ? new AccountOperationResult(true)
            : new AccountOperationResult(false, result.Errors.ToMessageKey());
    }

    private async Task<AccountOperationResult> CreateClientInternalAsync(
        string email,
        string firstName,
        string lastName,
        bool emailConfirmed,
        CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return new AccountOperationResult(false, MessageKeys.Account.EmailExists);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            UserType = UserType.Client,
            EmailConfirmed = emailConfirmed,
            IsActive = true
        };

        // Passwordless client: Identity requires a hash; use a random unusable password.
        var result = await userManager.CreateAsync(user, Guid.NewGuid().ToString("N") + "Aa1!");
        if (!result.Succeeded)
        {
            return new AccountOperationResult(false, result.Errors.ToMessageKey());
        }

        if (!await roleManager.RoleExistsAsync(ClientRoleName))
        {
            await roleManager.CreateAsync(new ApplicationRole(ClientRoleName, UserType.Client) { Id = Guid.NewGuid(), IsSystem = true });
        }

        await userManager.AddToRoleAsync(user, ClientRoleName);
        return new AccountOperationResult(true);
    }

    public async Task<CreateProviderResult> CreateProviderAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return new CreateProviderResult(false, Error: MessageKeys.Account.EmailExists);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            UserType = UserType.Provider,
            EmailConfirmed = true,
            IsActive = true,
            IsSystem = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return new CreateProviderResult(false, Error: result.Errors.ToMessageKey());
        }

        if (!await roleManager.RoleExistsAsync(ProviderRoleName))
        {
            await roleManager.CreateAsync(new ApplicationRole(ProviderRoleName, UserType.Provider) { Id = Guid.NewGuid(), IsSystem = true });
        }

        await userManager.AddToRoleAsync(user, ProviderRoleName);
        return new CreateProviderResult(true, user.Id);
    }

    public async Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        user.IsActive = isActive;
        await userManager.UpdateAsync(user);
    }

    public async Task ConfirmEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        return await userManager.CheckPasswordAsync(user, password);
    }

    public async Task AccessFailedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        await userManager.AccessFailedAsync(user);
    }

    public async Task ResetAccessFailedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        await userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task<bool> IsLockedOutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        return await userManager.IsLockedOutAsync(user);
    }

    public async Task<AccountOperationResult> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded
            ? new AccountOperationResult(true)
            : new AccountOperationResult(false, result.Errors.ToMessageKey());
    }

    public async Task<AccountOperationResult> ResetPasswordAsync(
        Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded
            ? new AccountOperationResult(true)
            : new AccountOperationResult(false, result.Errors.ToMessageKey());
    }

    public async Task UpdateProfileAsync(
        Guid userId, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        user.FirstName = firstName;
        user.LastName = lastName;
        await userManager.UpdateAsync(user);
    }

    public async Task<AccountOperationResult> ChangeEmailAsync(
        Guid userId, string newEmail, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        var normalized = userManager.NormalizeEmail(newEmail);
        if (await userManager.Users.AnyAsync(u => u.NormalizedEmail == normalized && u.Id != userId, cancellationToken))
        {
            return new AccountOperationResult(false, MessageKeys.Account.EmailExists);
        }

        user.Email = newEmail;
        user.UserName = newEmail;
        user.NormalizedEmail = normalized;
        user.NormalizedUserName = userManager.NormalizeName(newEmail);
        user.EmailConfirmed = true;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? new AccountOperationResult(true)
            : new AccountOperationResult(false, result.Errors.ToMessageKey());
    }

    public async Task EnsureUserTypeAsync(Guid userId, UserType expectedType, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId);
        if (user.UserType != expectedType || !user.IsActive)
        {
            throw new ForbiddenAccessException();
        }
    }

    public async Task DeleteByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(
            u => u.NormalizedEmail == userManager.NormalizeEmail(email), cancellationToken);
        if (user is null)
        {
            return;
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to delete account '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    private async Task<ApplicationUser> RequireUserAsync(Guid userId)
    {
        return await userManager.FindByIdAsync(userId.ToString())
            ?? throw NotFoundException.For(nameof(ApplicationUser), userId);
    }

    private async Task<AuthUserSnapshot> ToSnapshotAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var permissions = new List<string>();
        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;
            var claims = await roleManager.GetClaimsAsync(role);
            permissions.AddRange(claims.Where(c => c.Type == "permission").Select(c => c.Value));
        }

        return new AuthUserSnapshot(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.UserType,
            user.EmailConfirmed,
            user.IsActive,
            roles.ToList(),
            permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            user.IsSystem);
    }
}
