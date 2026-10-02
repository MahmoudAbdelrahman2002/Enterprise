using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Commands.UpdateProviderRole;

public sealed record UpdateProviderRoleCommand(
    Guid Id,
    LocalizedText Name,
    IReadOnlyList<string> Permissions) : IRequest<RoleDetailDto>;
