using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Clients.Queries.GetAdminClients;

public sealed class GetAdminClientsQueryHandler(IStaffManagerService staffManagerService)
    : IRequestHandler<GetAdminClientsQuery, PagedResult<AdminClientDto>>
{
    public async Task<PagedResult<AdminClientDto>> Handle(
        GetAdminClientsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await staffManagerService.GetStaffListAsync(
            UserType.Client,
            providerId: null,
            request,
            request.SearchTerm,
            roleId: null,
            request.IsActive,
            cancellationToken);

        var items = result.Items
            .Select(client => new AdminClientDto(
                client.Id,
                client.FirstName,
                client.LastName,
                client.Email,
                client.PhoneNumber,
                client.IsActive))
            .ToList();

        return new PagedResult<AdminClientDto>(items, result.TotalCount, result.PageNumber, result.PageSize);
    }
}
