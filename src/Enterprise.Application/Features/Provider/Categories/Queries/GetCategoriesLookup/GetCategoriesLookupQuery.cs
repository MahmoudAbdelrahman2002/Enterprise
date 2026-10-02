using Enterprise.Application.Features.Provider.Categories.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Queries.GetCategoriesLookup;

public sealed record GetCategoriesLookupQuery : IRequest<IReadOnlyList<CategoryLookupDto>>;
