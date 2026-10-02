using Enterprise.Application.Features.Provider.Categories.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.SetCategoryActive;

public sealed record SetCategoryActiveCommand(Guid Id, bool IsActive) : IRequest<CategoryDetailDto>;
