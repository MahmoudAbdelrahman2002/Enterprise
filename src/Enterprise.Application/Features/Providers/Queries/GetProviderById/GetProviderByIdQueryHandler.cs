using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Providers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Providers.Queries.GetProviderById;

public sealed class GetProviderByIdQueryHandler(
    IProviderAdminQueryService providerAdminQueryService,
    ILogger<GetProviderByIdQueryHandler> logger)
    : IRequestHandler<GetProviderByIdQuery, ProviderDto>
{
    public async Task<ProviderDto> Handle(GetProviderByIdQuery request, CancellationToken cancellationToken)
    {
        var detail = await providerAdminQueryService.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Provider", request.Id);

        logger.LogInformation("Fetched provider {ProviderId}", request.Id);
        return detail.ToDto();
    }
}
