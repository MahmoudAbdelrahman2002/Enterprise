using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Providers.Commands.UpdateProvider;

public sealed class UpdateProviderCommandBoundaryValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ServiceId).Must(value => !value.HasValue || value.Value != Guid.Empty).WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
