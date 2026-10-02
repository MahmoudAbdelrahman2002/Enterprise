using Asp.Versioning;
using Enterprise.Api.Authorization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Client.Payments.Commands.CreatePaymentCommand;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Controllers.V1.Client;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/{providerId:guid}/payments")]
[RequireClient]
public sealed class ClientPaymentController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreatePaymentSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CreatePaymentSessionDto>>> CreatePayment(
        [FromRoute] Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new CreatePaymentCommand(providerId), cancellationToken);
        return OkResponse(result, MessageKeys.Payment.Created);
    }
}
