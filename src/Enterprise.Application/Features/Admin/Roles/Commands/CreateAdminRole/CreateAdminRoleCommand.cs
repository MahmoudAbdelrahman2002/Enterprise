using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Commands.CreateAdminRole;

public sealed record CreateAdminRoleCommand(
    string Name,
    IReadOnlyList<string> Permissions) : IRequest<RoleDetailDto>;
