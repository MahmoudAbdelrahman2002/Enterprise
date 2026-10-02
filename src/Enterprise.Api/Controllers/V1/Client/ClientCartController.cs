using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.Cart.Commands.AddToCartCommand;
using Enterprise.Application.Features.Client.Cart.Commands.DeleteCartCommand;
using Enterprise.Application.Features.Client.Cart.Commands.DeleteCartItemCommand;
using Enterprise.Application.Features.Client.Cart.Commands.UpdateCartItemCommand;
using Enterprise.Application.Features.Client.Cart.Queries;
using Enterprise.Application.Features.Client.Cart.Queries.GetCartCommand;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/{providerId:guid}/cart")]
[RequireClient]
public sealed class ClientCartController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ShoppingCartDto>>> GetCart(
        [FromRoute] Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var request = new GetCartCommand(providerId);
        return OkResponse(
            await Mediator.Send(request, cancellationToken),
            MessageKeys.Cart.Retrieved);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ShoppingCartItemDto>>> AddToCart(
        [FromRoute] Guid providerId,
        [FromBody] AddToCartRequest body,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(
            new AddToCartCommand(providerId, body.ProductId, body.Quantity),
            cancellationToken);
        return OkResponse(result, MessageKeys.Cart.Added);
    }

    [HttpPut("items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ShoppingCartItemDto>>> UpdateCartItem(
        [FromRoute] Guid providerId,
        [FromRoute] Guid cartItemId,
        [FromBody] UpdateCartItemRequest body,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(
            new UpdateCartItemCommand(providerId, cartItemId, body.Quantity),
            cancellationToken);
        return OkResponse(result, MessageKeys.Cart.Updated);
    }

    [HttpDelete("items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteCartItem(
        [FromRoute] Guid providerId,
        [FromRoute] Guid cartItemId,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(
            new DeleteCartItemCommand(providerId, cartItemId),
            cancellationToken);
        return EmptyResponse(MessageKeys.Cart.ItemRemoved);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteCart(
        [FromRoute] Guid providerId,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new DeleteCartCommand(providerId), cancellationToken);
        return EmptyResponse(MessageKeys.Cart.Deleted);
    }
}

public sealed record AddToCartRequest(Guid ProductId, int Quantity);

public sealed record UpdateCartItemRequest(int Quantity);
