using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Common.Validation;

public static class LocalizedRuleExtensions
{
    public static IRuleBuilderOptions<T, string> Required<T>(
        this IRuleBuilder<T, string> rule, IAppLocalizer localizer) =>
        rule.NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);

    public static IRuleBuilderOptions<T, string> RequiredEmail<T>(
        this IRuleBuilder<T, string> rule, IAppLocalizer localizer) =>
        rule.NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .EmailAddress().WithMessage(_ => localizer[MessageKeys.Validation.Email]);

    public static IRuleBuilderOptions<T, string> MaxLen<T>(
        this IRuleBuilderOptions<T, string> rule, IAppLocalizer localizer, int max) =>
        rule.MaximumLength(max).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, max]);

    public static IRuleBuilderOptions<T, string> OtpCode<T>(
        this IRuleBuilder<T, string> rule, IAppLocalizer localizer) =>
        rule.NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .Length(4, 10).WithMessage(_ => localizer[MessageKeys.Validation.OtpLength, 4, 10]);

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(
        this IRuleBuilder<T, string> rule, IAppLocalizer localizer) =>
        rule.NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .MinimumLength(8).WithMessage(_ => localizer[MessageKeys.Validation.PasswordMinLength])
            .Matches("[A-Z]").WithMessage(_ => localizer[MessageKeys.Validation.PasswordUppercase])
            .Matches("[a-z]").WithMessage(_ => localizer[MessageKeys.Validation.PasswordLowercase])
            .Matches("[0-9]").WithMessage(_ => localizer[MessageKeys.Validation.PasswordDigit])
            .Matches("[^a-zA-Z0-9]").WithMessage(_ => localizer[MessageKeys.Validation.PasswordSpecial]);
}
