using Enterprise.Application.Features.Client.Category;
using MediatR;

namespace Enterprise.Application.Features.Client.Category.Queries.GetClientCategoryByIdQuery;

public sealed record GetClientCategoryByIdQuery(Guid ProviderId, Guid Id) : IRequest<ClientCategoryDto>;
