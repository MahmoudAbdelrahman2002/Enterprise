using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Commands.UpdateAdminRole;

public sealed record UpdateAdminRoleCommand(
    Guid Id,
    string Name,
    IReadOnlyList<string> Permissions) : IRequest<RoleDetailDto>;
