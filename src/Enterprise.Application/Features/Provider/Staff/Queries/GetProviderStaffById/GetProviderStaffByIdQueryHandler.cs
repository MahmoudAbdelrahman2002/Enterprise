using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Staff.Queries.GetProviderStaffById;

public sealed class GetProviderStaffByIdQueryHandler(
    IStaffManagerService staffManagerService,
    IProviderContext providerContext,
    ILogger<GetProviderStaffByIdQueryHandler> logger)
    : IRequestHandler<GetProviderStaffByIdQuery, StaffDetailDto>
{
    public async Task<StaffDetailDto> Handle(
        GetProviderStaffByIdQuery request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var staff = await staffManagerService.GetStaffByIdAsync(
            request.Id,
            UserType.Provider,
            providerId,
            cancellationToken);

        if (staff is null)
        {
            throw NotFoundException.For("ProviderStaff", request.Id);
        }

        logger.LogInformation("Fetched staff {StaffId} for provider {ProviderId}", staff.Id, providerId);
        return staff;
    }
}
