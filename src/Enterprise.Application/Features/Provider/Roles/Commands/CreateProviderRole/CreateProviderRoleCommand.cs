using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Commands.CreateProviderRole;

public sealed record CreateProviderRoleCommand(
    string Name,
    IReadOnlyList<string> Permissions) : IRequest<RoleDetailDto>;
