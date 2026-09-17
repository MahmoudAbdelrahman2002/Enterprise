using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Staff.Commands.UpdateProviderStaff;

public sealed record UpdateProviderStaffCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    Guid RoleId) : IRequest<StaffDetailDto>;
