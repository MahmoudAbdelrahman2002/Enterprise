using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Clients.Queries.GetAdminClientById;

public sealed class GetAdminClientByIdQueryHandler(IStaffManagerService staffManagerService)
    : IRequestHandler<GetAdminClientByIdQuery, AdminClientDto>
{
    public async Task<AdminClientDto> Handle(GetAdminClientByIdQuery request, CancellationToken cancellationToken)
    {
        var client = await staffManagerService.GetStaffByIdAsync(
            request.Id,
            UserType.Client,
            providerId: null,
            cancellationToken)
            ?? throw NotFoundException.For("Client", request.Id);

        return new AdminClientDto(
            client.Id,
            client.FirstName,
            client.LastName,
            client.Email,
            client.PhoneNumber,
            client.IsActive);
    }
}
