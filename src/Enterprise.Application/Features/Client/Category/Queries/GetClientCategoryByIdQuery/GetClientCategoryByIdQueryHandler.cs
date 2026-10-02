using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Client.Category;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Category.Queries.GetClientCategoryByIdQuery;

public sealed class GetClientCategoryByIdQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture culture,
    IClientProviderQueryService clientProviderQueryService)
    : IRequestHandler<GetClientCategoryByIdQuery, ClientCategoryDto>
{
    public async Task<ClientCategoryDto> Handle(
        GetClientCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        _ = await clientProviderQueryService.GetByIdAsync(request.ProviderId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), request.ProviderId);

        var language = culture.LanguageCode;
        var category = await unitOfWork.Categories.GetByIdAndProviderIdAsync(
            request.Id, request.ProviderId, cancellationToken);

        if (category is null || !category.IsActive)
        {
            throw NotFoundException.For(nameof(Category), request.Id);
        }

        return category.ClientCategoryToDto(language);
    }
}
