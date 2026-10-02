using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Services.Queries.GetServicesLookup;

public sealed class GetServicesLookupQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture culture,
    ILogger<GetServicesLookupQueryHandler> logger)
    : IRequestHandler<GetServicesLookupQuery, IReadOnlyList<MarketplaceServiceLookupDto>>
{
    public async Task<IReadOnlyList<MarketplaceServiceLookupDto>> Handle(
        GetServicesLookupQuery request, CancellationToken cancellationToken)
    {
        var language = culture.LanguageCode;
        var spec = MarketplaceServiceFilterSpecification.ForLookup(language);
        var services = await unitOfWork.Services.ListAsync(spec, cancellationToken);

        var result = services.Select(s => s.ToLookupDto(language)).ToList();
        logger.LogInformation("Listed {Count} marketplace service lookups", result.Count);
        return result;
    }
}
