using MediatR;

namespace Enterprise.Application.Features.Admin.Clients.Commands.SetAdminClientActive;

public sealed record SetAdminClientActiveCommand(Guid Id, bool IsActive) : IRequest;
