using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.DeleteCategoryCommand;

public class  DeleteCategoryCommand(Guid categoryId) : IRequest
{
    public Guid CategoryId { get; } = categoryId;
}
