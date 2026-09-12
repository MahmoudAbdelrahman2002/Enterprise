using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Auth;

namespace Enterprise.Application.Features.Auth.Common;

public interface ITokenIssuanceService
{
    Task<AuthResponseDto> IssueTokensAsync(
        AuthUserSnapshot user, string? ipAddress, CancellationToken cancellationToken = default);
}
