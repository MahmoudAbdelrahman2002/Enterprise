using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Users.Commands.CreateAdminUser;

public sealed record CreateAdminUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    Guid RoleId) : IRequest<StaffDetailDto>;
