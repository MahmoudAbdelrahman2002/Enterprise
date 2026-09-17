using Enterprise.Application.Features.Provider.Categories.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Queries.GetCategoriesListQuery;

public class GetCategoriesListQuery : IRequest<IReadOnlyList<CategoryDetailDto>>
{
    
}
