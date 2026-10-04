using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Common.Validation;

public static class LocalizedRuleExtensions
{
    public static IRuleBuilderOptions<T, string> Required<T>(
        this IRuleBuilderInitial<T, string> rule, IAppLocalizer localizer) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .Must(value => FieldFormats.PlainText(value)).WithMessage(_ => localizer[MessageKeys.Validation.TextFormat]);

    public static IRuleBuilderOptions<T, string> RequiredEmail<T>(
        this IRuleBuilderInitial<T, string> rule, IAppLocalizer localizer) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .MaximumLength(ValidationPolicy.EmailMax).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, ValidationPolicy.EmailMax])
            .Must(FieldFormats.Email).WithMessage(_ => localizer[MessageKeys.Validation.Email]);

    public static IRuleBuilderOptions<T, string> MaxLen<T>(
        this IRuleBuilderOptions<T, string> rule, IAppLocalizer localizer, int max) =>
        rule.MaximumLength(max).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, max]);

    public static IRuleBuilderOptions<T, string> OtpCode<T>(
        this IRuleBuilderInitial<T, string> rule, IAppLocalizer localizer) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .Matches($"^[0-9]{{{ValidationPolicy.OtpLength}}}$").WithMessage(_ => localizer[MessageKeys.Validation.OtpFormat, ValidationPolicy.OtpLength]);

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(
        this IRuleBuilderInitial<T, string> rule, IAppLocalizer localizer) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .MaximumLength(ValidationPolicy.PasswordMax).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, ValidationPolicy.PasswordMax])
            .Must(value => FieldFormats.PlainText(value)).WithMessage(_ => localizer[MessageKeys.Validation.TextFormat])
            .MinimumLength(ValidationPolicy.PasswordMin).WithMessage(_ => localizer[MessageKeys.Validation.PasswordMinLength])
            .Matches("[A-Z]").WithMessage(_ => localizer[MessageKeys.Validation.PasswordUppercase])
            .Matches("[a-z]").WithMessage(_ => localizer[MessageKeys.Validation.PasswordLowercase])
            .Matches("[0-9]").WithMessage(_ => localizer[MessageKeys.Validation.PasswordDigit])
            .Matches("[^a-zA-Z0-9]").WithMessage(_ => localizer[MessageKeys.Validation.PasswordSpecial]);
    public static IRuleBuilderOptions<T, string> PersonName<T>(this IRuleBuilderInitial<T, string> rule, IAppLocalizer localizer, int max) =>
        rule.Required(localizer).MaximumLength(max).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, max])
            .Must(FieldFormats.PersonName).WithMessage(_ => localizer[MessageKeys.Validation.NameFormat]);

    public static IRuleBuilderOptions<T, string?> Phone<T>(this IRuleBuilderInitial<T, string?> rule, IAppLocalizer localizer) =>
        rule.Cascade(CascadeMode.Stop).MaximumLength(ValidationPolicy.PhoneMax).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, ValidationPolicy.PhoneMax])
            .Must(FieldFormats.Phone).WithMessage(_ => localizer[MessageKeys.Validation.PhoneFormat]);

    public static IRuleBuilderOptions<T, string> ExistingPassword<T>(this IRuleBuilderInitial<T, string> rule, IAppLocalizer localizer) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .MaximumLength(ValidationPolicy.PasswordMax).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, ValidationPolicy.PasswordMax])
            .Must(value => FieldFormats.PlainText(value)).WithMessage(_ => localizer[MessageKeys.Validation.TextFormat]);
}
