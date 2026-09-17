using Enterprise.Application.Features.Provider.Categories.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.UpdateCategoryCommand;

public record UpdateCategoryCommand(Guid Id, UpdateCategoryDto Command) : IRequest<CategoryDetailDto>
{
    
}
