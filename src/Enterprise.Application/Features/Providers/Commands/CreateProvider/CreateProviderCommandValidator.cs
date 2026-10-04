using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Providers.Commands.CreateProvider;

public sealed class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    public CreateProviderCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
        RuleFor(x => x.Password).StrongPassword(localizer);
        RuleFor(x => x.FirstName).PersonName(localizer, ValidationPolicy.NameMax);
        RuleFor(x => x.LastName).PersonName(localizer, ValidationPolicy.NameMax);
        RuleFor(x => x.CompanyName).Required(localizer).MaxLen(localizer, ValidationPolicy.CompanyMax);
        RuleFor(x => x.PhoneNumber).Phone(localizer);
        RuleFor(x => x.ServiceId!)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .When(x => x.ServiceId.HasValue);
    }
}
