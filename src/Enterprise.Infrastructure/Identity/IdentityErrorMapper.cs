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
            _ => errors.FirstOrDefault()?.Description ?? MessageKeys.Account.UnableToComplete
        };
    }

    public static IReadOnlyList<string> ToMessageKeysOrDescriptions(this IEnumerable<IdentityError> errors)
    {
        return errors.Select(e => e.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName" => MessageKeys.Account.EmailExists,
            "PasswordTooShort" => MessageKeys.Validation.PasswordMinLength,
            "PasswordRequiresDigit" => MessageKeys.Validation.PasswordDigit,
            "PasswordRequiresLower" => MessageKeys.Validation.PasswordLowercase,
            "PasswordRequiresUpper" => MessageKeys.Validation.PasswordUppercase,
            "PasswordRequiresNonAlphanumeric" => MessageKeys.Validation.PasswordSpecial,
            "PasswordMismatch" => MessageKeys.Auth.InvalidCredentials,
            _ => string.IsNullOrWhiteSpace(e.Description) ? MessageKeys.Account.UnableToComplete : e.Description
        }).Distinct().ToList();
    }
}
