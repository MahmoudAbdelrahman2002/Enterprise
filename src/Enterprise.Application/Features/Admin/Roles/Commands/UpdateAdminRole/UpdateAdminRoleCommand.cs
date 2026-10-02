using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Commands.UpdateAdminRole;

public sealed record UpdateAdminRoleCommand(
    Guid Id,
    LocalizedText Name,
    IReadOnlyList<string> Permissions) : IRequest<RoleDetailDto>;
