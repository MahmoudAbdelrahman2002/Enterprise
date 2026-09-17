using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Services;
using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Queries.GetAdminServicesList;

public sealed class GetAdminServicesListQueryHandler(IUnitOfWork unitOfWork, ICurrentCulture culture)
    : IRequestHandler<GetAdminServicesListQuery, PagedResult<MarketplaceServiceDto>>
{
    public async Task<PagedResult<MarketplaceServiceDto>> Handle(
        GetAdminServicesListQuery request, CancellationToken cancellationToken)
    {
        var language = culture.LanguageCode;
        var countSpec = MarketplaceServiceFilterSpecification.ForCount(
            request.SearchTerm, request.IsActive, language);
        var totalCount = await unitOfWork.Services.CountAsync(countSpec, cancellationToken);

        var pageSpec = MarketplaceServiceFilterSpecification.ForPage(
            request.SearchTerm, request.IsActive, request.PageNumber, request.PageSize, language);
        var services = await unitOfWork.Services.ListAsync(pageSpec, cancellationToken);

        var items = services.Select(s => s.ToDto(language)).ToList();

        return new PagedResult<MarketplaceServiceDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
