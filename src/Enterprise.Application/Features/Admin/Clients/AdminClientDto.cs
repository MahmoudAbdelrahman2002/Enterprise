namespace Enterprise.Application.Features.Admin.Clients;

public sealed record AdminClientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsActive);
