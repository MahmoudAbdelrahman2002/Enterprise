using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.Cart.Queries;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Cart.Queries.GetCartCommand;

public sealed class GetCartCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ICurrentCulture currentCulture) : IRequestHandler<GetCartCommand, ShoppingCartDto>
{
    public async Task<ShoppingCartDto> Handle(GetCartCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var cart = await unitOfWork.Carts.GetCartByProviderIdAndUserIdAsync(
            request.ProviderId, userId, cancellationToken);

        if (cart is null)
        {
            return new ShoppingCartDto
            {
                Id = Guid.Empty,
                ProviderId = request.ProviderId,
                ProviderName = string.Empty,
                UserId = userId,
                Items = [],
                TotalPrice = 0m
            };
        }

        return cart.ToDto(currentCulture.LanguageCode);
    }
}
