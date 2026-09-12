using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using FluentValidation;

namespace Enterprise.Application.Common.Validation;

public sealed class LocalizedTextValidator : AbstractValidator<LocalizedText>
{
    public LocalizedTextValidator(IAppLocalizer localizer, int maxLength, bool englishRequired)
    {
        if (englishRequired)
        {
            RuleFor(x => x.En)
                .NotEmpty()
                .WithMessage(_ => localizer[MessageKeys.Validation.LocalizedEnRequired])
                .MaximumLength(maxLength)
                .WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, maxLength]);
        }
        else
        {
            RuleFor(x => x.En)
                .MaximumLength(maxLength)
                .WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, maxLength]);
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
