using Enterprise.Application.Features.Provider.Categories.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.DeleteCategoryImage;

public sealed record DeleteCategoryImageCommand(Guid Id) : IRequest<CategoryDetailDto>;
