using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Queries.GetAdminRoleById;

public sealed record GetAdminRoleByIdQuery(Guid Id) : IRequest<RoleDetailDto>;
