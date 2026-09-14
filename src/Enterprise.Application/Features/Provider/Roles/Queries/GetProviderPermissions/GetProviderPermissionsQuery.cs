using Enterprise.Application.Common.Authorization;
using MediatR;

namespace Enterprise.Application.Features.Provider.Roles.Queries.GetProviderPermissions;

public sealed record GetProviderPermissionsQuery : IRequest<IReadOnlyList<PermissionGroupDto>>;
