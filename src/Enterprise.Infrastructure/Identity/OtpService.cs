using System.Security.Cryptography;
using System.Text;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Settings;
using Enterprise.Domain.Enums;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Enterprise.Infrastructure.Identity;

public sealed class OtpService(
    ApplicationDbContext dbContext,
    IOptions<OtpSettings> otpOptions) : IOtpService
{
    private readonly OtpSettings _settings = otpOptions.Value;

    public async Task<string> IssueAsync(string email, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var active = await dbContext.OtpChallenges
            .Where(o => o.Email == normalizedEmail && o.Purpose == purpose && o.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var challenge in active)
        {
            challenge.Consume();
        }

        var code = GenerateNumericCode(_settings.Length);
        var entity = new OtpChallenge(
            normalizedEmail,
            purpose,
            Hash(code),
            DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes));

        dbContext.OtpChallenges.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return code;
    }

    public async Task InvalidateAsync(
        string email, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var active = await dbContext.OtpChallenges
            .Where(o => o.Email == normalizedEmail && o.Purpose == purpose && o.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (active.Count == 0)
        {
            return;
        }

        foreach (var challenge in active)
        {
            challenge.Consume();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> VerifyAndConsumeAsync(
        string email, OtpPurpose purpose, string code, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var challenge = await dbContext.OtpChallenges
            .Where(o => o.Email == normalizedEmail && o.Purpose == purpose && o.ConsumedAtUtc == null)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge is null || challenge.IsExpired)
        {
            return false;
        }

        if (challenge.Attempts >= _settings.MaxAttempts)
        {
            challenge.Consume();
            await dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        challenge.IncrementAttempts();

        if (!FixedTimeEquals(challenge.CodeHash, Hash(code)))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        challenge.Consume();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateNumericCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString($"D{length}");
    }

    private static string Hash(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
