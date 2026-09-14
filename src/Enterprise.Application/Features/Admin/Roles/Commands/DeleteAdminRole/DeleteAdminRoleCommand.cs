using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Commands.DeleteAdminRole;

public sealed record DeleteAdminRoleCommand(Guid Id) : IRequest;
