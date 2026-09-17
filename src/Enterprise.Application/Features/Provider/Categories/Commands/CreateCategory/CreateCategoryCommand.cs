using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    LocalizedText Name,
    LocalizedText? Description = null,
    int DisplayOrder = 0,
    bool IsActive = true) : IRequest<CategoryDetailDto>;
