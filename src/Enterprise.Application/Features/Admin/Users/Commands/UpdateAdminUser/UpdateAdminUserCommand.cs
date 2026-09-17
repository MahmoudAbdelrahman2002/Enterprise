using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Users.Commands.UpdateAdminUser;

public sealed record UpdateAdminUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    Guid RoleId) : IRequest<StaffDetailDto>;
