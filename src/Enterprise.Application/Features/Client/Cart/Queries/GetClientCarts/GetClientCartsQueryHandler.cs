using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.Cart.Queries;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Cart.Queries.GetClientCarts;

public sealed class GetClientCartsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ICurrentCulture currentCulture) : IRequestHandler<GetClientCartsQuery, IReadOnlyList<ShoppingCartDto>>
{
    public async Task<IReadOnlyList<ShoppingCartDto>> Handle(
        GetClientCartsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var carts = await unitOfWork.Carts.ListByUserIdAsync(userId, cancellationToken);
        return carts.Select(c => c.ToDto(currentCulture.LanguageCode)).ToList();
    }
}
