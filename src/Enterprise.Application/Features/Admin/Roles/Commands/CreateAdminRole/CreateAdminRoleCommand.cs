using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Commands.CreateAdminRole;

public sealed record CreateAdminRoleCommand(
    LocalizedText Name,
    IReadOnlyList<string> Permissions) : IRequest<RoleDetailDto>;
