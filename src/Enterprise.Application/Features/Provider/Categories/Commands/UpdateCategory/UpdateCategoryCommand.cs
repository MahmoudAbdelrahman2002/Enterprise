using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id,
    LocalizedText Name,
    LocalizedText? Description = null,
    int DisplayOrder = 0) : IRequest<CategoryDetailDto>;
