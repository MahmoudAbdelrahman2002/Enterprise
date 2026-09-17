using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Staff.Commands.CreateProviderStaff;

public sealed record CreateProviderStaffCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    Guid RoleId) : IRequest<StaffDetailDto>;
