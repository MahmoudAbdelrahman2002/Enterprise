using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Providers.Commands.CreateProvider;

public sealed class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    public CreateProviderCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer).MaxLen(localizer, 256);
        RuleFor(x => x.Password).StrongPassword(localizer);
        RuleFor(x => x.FirstName).Required(localizer).MaxLen(localizer, 100);
        RuleFor(x => x.LastName).Required(localizer).MaxLen(localizer, 100);
        RuleFor(x => x.CompanyName).Required(localizer).MaxLen(localizer, 200);
        RuleFor(x => x.PhoneNumber!)
            .MaximumLength(40)
            .WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, 40])
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
