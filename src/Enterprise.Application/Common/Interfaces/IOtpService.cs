using Enterprise.Domain.Enums;

namespace Enterprise.Application.Common.Interfaces;

public interface IOtpService
{
    /// <summary>Creates a new OTP for the purpose, invalidates prior active codes for the same email+purpose, and returns the raw code (to email).</summary>
    Task<string> IssueAsync(string email, OtpPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>Validates and consumes a matching unexpired OTP. Returns false on mismatch/expiry/max attempts.</summary>
    Task<bool> VerifyAndConsumeAsync(string email, OtpPurpose purpose, string code, CancellationToken cancellationToken = default);

    /// <summary>Consumes all active OTPs for the email+purpose (compensating action when email delivery fails).</summary>
    Task InvalidateAsync(string email, OtpPurpose purpose, CancellationToken cancellationToken = default);
}
