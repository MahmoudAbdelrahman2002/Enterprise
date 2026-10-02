using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Staff.Queries.GetProviderStaffList;

public sealed class GetProviderStaffListQueryHandler(
    IStaffManagerService staffManagerService,
    IProviderContext providerContext,
    ILogger<GetProviderStaffListQueryHandler> logger)
    : IRequestHandler<GetProviderStaffListQuery, PagedResult<StaffListItemDto>>
{
    public async Task<PagedResult<StaffListItemDto>> Handle(
        GetProviderStaffListQuery request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var result = await staffManagerService.GetStaffListAsync(
            UserType.Provider,
            providerId,
            request,
            request.SearchTerm,
            request.RoleId,
            request.IsActive,
            cancellationToken);

        logger.LogInformation(
            "Listed {Count} staff (total {TotalCount}) for provider {ProviderId}",
            result.Items.Count,
            result.TotalCount,
            providerId);
        return result;
    }
}
