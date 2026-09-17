using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Staff.Queries.GetProviderStaffById;

public sealed record GetProviderStaffByIdQuery(Guid Id) : IRequest<StaffDetailDto>;
