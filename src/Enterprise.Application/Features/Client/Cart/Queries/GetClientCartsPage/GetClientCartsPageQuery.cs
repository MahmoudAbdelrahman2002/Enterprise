using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Cart.Queries.GetClientCartsPage;

public sealed record GetClientCartsPageQuery : PaginationParams, IRequest<PagedResult<ShoppingCartDto>>;

public sealed class GetClientCartsPageQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, ICurrentCulture culture)
    : IRequestHandler<GetClientCartsPageQuery, PagedResult<ShoppingCartDto>>
{
    public async Task<PagedResult<ShoppingCartDto>> Handle(GetClientCartsPageQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);
        var count = await unitOfWork.Carts.CountByUserIdAsync(userId, cancellationToken);
        var carts = await unitOfWork.Carts.ListPageByUserIdAsync(userId, request.PageNumber, request.PageSize, cancellationToken);
        return new PagedResult<ShoppingCartDto>(carts.Select(cart => cart.ToDto(culture.LanguageCode)).ToArray(), count, request.PageNumber, request.PageSize);
    }
}

public sealed record GetClientCartCountQuery : IRequest<ClientCartCountDto>;
public sealed record ClientCartCountDto(int TotalQuantity);

public sealed class GetClientCartCountQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetClientCartCountQuery, ClientCartCountDto>
{
    public async Task<ClientCartCountDto> Handle(GetClientCartCountQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);
        return new ClientCartCountDto(await unitOfWork.Carts.CountUnitsByUserIdAsync(userId, cancellationToken));
    }
}
