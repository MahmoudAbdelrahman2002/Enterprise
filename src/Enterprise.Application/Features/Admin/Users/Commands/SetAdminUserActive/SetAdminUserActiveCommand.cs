using MediatR;

namespace Enterprise.Application.Features.Admin.Users.Commands.SetAdminUserActive;

public sealed record SetAdminUserActiveCommand(Guid Id, bool IsActive) : IRequest;
