using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderRoleById;

public sealed record GetProviderRoleByIdQuery(Guid Id) : IRequest<RoleDetailDto>;
