using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;

namespace Enterprise.Application.Common.Interfaces;

public sealed record AccountOperationResult(bool Succeeded, string? Error = null, IReadOnlyList<string>? Errors = null);

public sealed record CreateProviderResult(bool Succeeded, Guid? UserId = null, string? Error = null, IReadOnlyList<string>? Errors = null);

/// <summary>
/// Facade over ASP.NET Core Identity so Application handlers never take a dependency on
/// <c>UserManager&lt;T&gt;</c> / Identity entity types directly.
/// </summary>
public interface IUserAccountService
{
    Task<AuthUserSnapshot?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthUserSnapshot?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AuthUserSnapshot?> FindByLoginAsync(
        string loginProvider, string providerKey, CancellationToken cancellationToken = default);

    Task<AccountOperationResult> CreateClientAsync(
        string email, string firstName, string lastName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Client with email already confirmed (social providers).
    /// </summary>
    Task<AccountOperationResult> CreateClientFromExternalAsync(
        string email, string firstName, string lastName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Provider account with a real password (admin-provisioned). Email is confirmed.
    /// </summary>
    Task<CreateProviderResult> CreateProviderAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);

    Task<AccountOperationResult> AddLoginAsync(
        Guid userId,
        string loginProvider,
        string providerKey,
        string? displayName = null,
        CancellationToken cancellationToken = default);

    Task ConfirmEmailAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);
    Task AccessFailedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ResetAccessFailedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsLockedOutAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AccountOperationResult> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    Task<AccountOperationResult> ResetPasswordAsync(
        Guid userId, string newPassword, CancellationToken cancellationToken = default);

    Task UpdateProfileAsync(
        Guid userId, string firstName, string lastName, CancellationToken cancellationToken = default);

    Task<AccountOperationResult> ChangeEmailAsync(
        Guid userId, string newEmail, CancellationToken cancellationToken = default);

    Task EnsureUserTypeAsync(Guid userId, UserType expectedType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes the user and related Identity rows. Used to roll back a failed registration
    /// when the account was created but the verification email could not be sent.
    /// </summary>
    Task DeleteByEmailAsync(string email, CancellationToken cancellationToken = default);
}
