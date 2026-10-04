using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using FluentValidation;

namespace Enterprise.Application.Common.Validation;

public sealed class LocalizedTextValidator : AbstractValidator<LocalizedText>
{
    public LocalizedTextValidator(IAppLocalizer localizer, int maxLength, bool englishRequired)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        if (englishRequired)
        {
            RuleFor(x => x.En)
                .NotEmpty()
                .WithMessage(_ => localizer[MessageKeys.Validation.LocalizedEnRequired])
                .MaximumLength(maxLength)
                .WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, maxLength])
                .Must(value => FieldFormats.PlainText(value, !englishRequired)).WithMessage(_ => localizer[MessageKeys.Validation.TextFormat]);
        }
        else
        {
            RuleFor(x => x.En)
                .MaximumLength(maxLength)
                .WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, maxLength])
                .Must(value => FieldFormats.PlainText(value, !englishRequired)).WithMessage(_ => localizer[MessageKeys.Validation.TextFormat]);
        }

        foreach (var language in new[] { "En", "It", "Ar" })
        {
            RuleFor(x => language == "En" ? x.En : language == "It" ? x.It : x.Ar)
                .Must(value => value is null || value.Length == 0 || (!string.IsNullOrWhiteSpace(value) && FieldFormats.PlainText(value, !englishRequired)))
                .WithMessage(_ => localizer[MessageKeys.Validation.TextFormat])
                .OverridePropertyName(language);
        }

        RuleFor(x => x.It)
            .MaximumLength(maxLength)
            .WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, maxLength])
            .When(x => x.It is not null);

        RuleFor(x => x.Ar)
            .MaximumLength(maxLength)
            .WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, maxLength])
            .When(x => x.Ar is not null);
    }
}
