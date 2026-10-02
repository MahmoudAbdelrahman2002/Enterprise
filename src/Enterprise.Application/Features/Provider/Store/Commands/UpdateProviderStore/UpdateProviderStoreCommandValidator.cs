using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Store.Commands.UpdateProviderStore;

public sealed class UpdateProviderStoreCommandValidator : AbstractValidator<UpdateProviderStoreCommand>
{
    public UpdateProviderStoreCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CompanyName).Required(localizer).MaxLen(localizer, 200);
        RuleFor(x => x.PhoneNumber!)
            .MaximumLength(40)
            .WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, 40])
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
