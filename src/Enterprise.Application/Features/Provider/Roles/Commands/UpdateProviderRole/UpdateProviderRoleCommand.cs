using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Commands.UpdateProviderRole;

public sealed record UpdateProviderRoleCommand(
    Guid Id,
    string Name,
    IReadOnlyList<string> Permissions) : IRequest<RoleDetailDto>;
