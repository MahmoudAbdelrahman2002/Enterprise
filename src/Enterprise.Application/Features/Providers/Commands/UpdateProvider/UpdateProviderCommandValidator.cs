using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Providers.Commands.UpdateProvider;

public sealed class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.FirstName).Required(localizer).MaxLen(localizer, 100);
        RuleFor(x => x.LastName).Required(localizer).MaxLen(localizer, 100);
        RuleFor(x => x.CompanyName).Required(localizer).MaxLen(localizer, 200);
        RuleFor(x => x.PhoneNumber!)
            .MaximumLength(40)
            .WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, 40])
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.ServiceId!)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .When(x => x.ServiceId.HasValue);
    }
}
