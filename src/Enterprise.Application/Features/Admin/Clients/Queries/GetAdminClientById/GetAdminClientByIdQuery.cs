using MediatR;

namespace Enterprise.Application.Features.Admin.Clients.Queries.GetAdminClientById;

public sealed record GetAdminClientByIdQuery(Guid Id) : IRequest<AdminClientDto>;
