using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Enterprise.Application.Features.Client.WebHook.Commands.HandleStripeWebhookCommand;

namespace Enterprise.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/webhooks")]
public sealed class StripeWebhooksController : ApiControllerBase
{
    [RequestSizeLimit(Enterprise.Application.Common.Validation.ValidationPolicy.WebhookMaxBytes)]
    [HttpPost("stripe")]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await Mediator.Send(new HandleStripeWebhookCommand(json, signature), cancellationToken);
        return Ok();
    }
}