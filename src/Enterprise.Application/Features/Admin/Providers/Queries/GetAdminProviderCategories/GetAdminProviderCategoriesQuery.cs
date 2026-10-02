using Enterprise.Application.Features.Provider.Categories.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Providers.Queries.GetAdminProviderCategories;

public sealed record GetAdminProviderCategoriesQuery(Guid ProviderId) : IRequest<IReadOnlyList<CategoryDetailDto>>;
