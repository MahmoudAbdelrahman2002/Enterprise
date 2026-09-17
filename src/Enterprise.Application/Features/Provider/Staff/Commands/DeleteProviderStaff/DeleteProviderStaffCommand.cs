using MediatR;

namespace Enterprise.Application.Features.Provider.Staff.Commands.DeleteProviderStaff;

public sealed record DeleteProviderStaffCommand(Guid Id) : IRequest;
