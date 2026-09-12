namespace Enterprise.Application.Features.Auth;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string UserType,
    IReadOnlyCollection<string> Roles);

public sealed record AuthResponseDto(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    UserDto User);

public sealed record ProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string UserType,
    bool EmailConfirmed);
