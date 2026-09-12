using Enterprise.Application.Common.Localization;
using Microsoft.AspNetCore.Identity;

namespace Enterprise.Infrastructure.Identity;

internal static class IdentityErrorMapper
{
    public static string ToMessageKey(this IEnumerable<IdentityError> errors)
    {
        var code = errors.FirstOrDefault()?.Code;
        return code switch
        {
            "DuplicateEmail" or "DuplicateUserName" => MessageKeys.Account.EmailExists,
            "PasswordTooShort" => MessageKeys.Validation.PasswordMinLength,
            "PasswordRequiresDigit" => MessageKeys.Validation.PasswordDigit,
            "PasswordRequiresLower" => MessageKeys.Validation.PasswordLowercase,
            "PasswordRequiresUpper" => MessageKeys.Validation.PasswordUppercase,
            "PasswordRequiresNonAlphanumeric" => MessageKeys.Validation.PasswordSpecial,
            "PasswordMismatch" => MessageKeys.Auth.InvalidCredentials,
            _ => MessageKeys.Account.UnableToComplete
        };
    }
}
