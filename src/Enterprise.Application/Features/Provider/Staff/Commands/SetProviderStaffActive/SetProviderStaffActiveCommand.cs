using MediatR;

namespace Enterprise.Application.Features.Provider.Staff.Commands.SetProviderStaffActive;

public sealed record SetProviderStaffActiveCommand(Guid Id, bool IsActive) : IRequest;
