using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Commands.DeleteProviderRole;

public sealed record DeleteProviderRoleCommand(Guid Id) : IRequest;
