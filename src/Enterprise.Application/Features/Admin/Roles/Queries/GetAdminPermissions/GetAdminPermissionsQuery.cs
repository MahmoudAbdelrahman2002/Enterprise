using Enterprise.Application.Common.Authorization;
using MediatR;

namespace Enterprise.Application.Features.Admin.Roles.Queries.GetAdminPermissions;

public sealed record GetAdminPermissionsQuery : IRequest<IReadOnlyList<PermissionGroupDto>>;
