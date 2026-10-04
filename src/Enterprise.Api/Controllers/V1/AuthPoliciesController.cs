using Asp.Versioning;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Enterprise.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth/verification-policy")]
[AllowAnonymous]
public sealed class AuthPoliciesController(IOptions<OtpSettings> settings) : ApiControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<VerificationPolicyDto>> Get() =>
        OkResponseText(new VerificationPolicyDto(settings.Value.Length, settings.Value.ExpirationMinutes, settings.Value.MaxAttempts, 30), "OK");
}

public sealed record VerificationPolicyDto(int CodeLength, int ExpirationMinutes, int MaxAttempts, int ResendWaitSeconds);
