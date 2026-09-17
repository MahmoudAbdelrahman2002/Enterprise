using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Services;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Queries.GetServicesLookup;

public sealed class GetServicesLookupQueryHandler(IUnitOfWork unitOfWork, ICurrentCulture culture)
    : IRequestHandler<GetServicesLookupQuery, IReadOnlyList<MarketplaceServiceLookupDto>>
{
    public async Task<IReadOnlyList<MarketplaceServiceLookupDto>> Handle(
        GetServicesLookupQuery request, CancellationToken cancellationToken)
    {
        var language = culture.LanguageCode;
        var spec = MarketplaceServiceFilterSpecification.ForLookup(language);
        var services = await unitOfWork.Services.ListAsync(spec, cancellationToken);

        return services.Select(s => s.ToLookupDto(language)).ToList();
    }
}
