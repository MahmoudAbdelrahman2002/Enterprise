using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Cart.Commands.DeleteCartCommand;

public sealed class DeleteCartCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteCartCommand>
{
    public async Task Handle(DeleteCartCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var cart = await unitOfWork.Carts.GetCartByProviderIdAndUserIdAsync(
            request.ProviderId, userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ShoppingCart), request.ProviderId);

        unitOfWork.Carts.Remove(cart);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
