using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.DeleteCategoryCommand;

public class DeleteCategoryCommandHandler(IUnitOfWork unitOfWork
,ICurrentUserService currentUserService) : IRequestHandler<DeleteCategoryCommand>
{
   async Task IRequestHandler<DeleteCategoryCommand>.Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var providerId = await ResolveProviderIdAsync(cancellationToken);
        

        var category = await unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || category.ProviderId != providerId)
        {
            throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Category), request.CategoryId);
        }
        unitOfWork.Categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

      private async Task<Guid> ResolveProviderIdAsync(CancellationToken cancellationToken)
    {
        if (currentUserService.ProviderId.HasValue)
        {
            return currentUserService.ProviderId.Value;
        }

        var userId = currentUserService.UserId ?? throw new ForbiddenAccessException();
        var provider = await unitOfWork.Providers.GetByUserIdAsync(userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Provider), userId);

        return provider.Id;
    }
}