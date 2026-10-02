using Enterprise.Application.Features.Provider.Categories.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Queries.GetCategoriesList;

public sealed record GetCategoriesListQuery(
    bool? IsActive = null) : IRequest<IReadOnlyList<CategoryDetailDto>>;
