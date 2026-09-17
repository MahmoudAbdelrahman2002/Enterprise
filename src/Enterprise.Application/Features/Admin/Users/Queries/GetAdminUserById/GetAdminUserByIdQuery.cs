using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Users.Queries.GetAdminUserById;

public sealed record GetAdminUserByIdQuery(Guid Id) : IRequest<StaffDetailDto>;
