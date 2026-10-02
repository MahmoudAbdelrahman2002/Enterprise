using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Services;
using MediatR;

namespace Enterprise.Application.Features.Client.Services.Queries.GetAllServicesQuery;

public class GetAllServicesQueryHandler(
IUnitOfWork unitOfWork,
ICurrentCulture Culture
) : IRequestHandler<GetAllServicesQuery, PagedResult<ClientMarketServiceDto>>
{
    public async Task<PagedResult<ClientMarketServiceDto>> Handle(GetAllServicesQuery request, CancellationToken cancellationToken)
    {
       var language = Culture.LanguageCode;
        var countSpec = MarketplaceServiceFilterSpecification.ForCount(
            request.SearchTerm, request.IsActive, language);
        var totalCount = await unitOfWork.Services.CountAsync(countSpec, cancellationToken);

        var pageSpec = MarketplaceServiceFilterSpecification.ForPage(
            request.SearchTerm, request.IsActive, request.PageNumber, request.PageSize, language);
        var services = await unitOfWork.Services.ListAsync(pageSpec, cancellationToken);
        var items = services.Select(service => service.ClientMarketToDto(language)).ToList();
        return new PagedResult<ClientMarketServiceDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}