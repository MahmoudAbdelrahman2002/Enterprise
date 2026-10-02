using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Commands.CreateProviderRole;

public sealed record CreateProviderRoleCommand(
    LocalizedText Name,
    IReadOnlyList<string> Permissions) : IRequest<RoleDetailDto>;
