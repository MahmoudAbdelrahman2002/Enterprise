using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Providers;
using MediatR;

namespace Enterprise.Application.Features.Providers.Queries.GetProviderById;

public sealed class GetProviderByIdQueryHandler(IProviderAdminQueryService providerAdminQueryService)
    : IRequestHandler<GetProviderByIdQuery, ProviderDto>
{
    public async Task<ProviderDto> Handle(GetProviderByIdQuery request, CancellationToken cancellationToken)
    {
        var detail = await providerAdminQueryService.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Provider", request.Id);

        return detail.ToDto();
    }
}
