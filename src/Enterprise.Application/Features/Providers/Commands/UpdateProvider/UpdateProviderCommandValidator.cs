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
