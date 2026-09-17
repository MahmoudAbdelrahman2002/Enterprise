using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Staff.Commands.SetProviderStaffActive;

public sealed class SetProviderStaffActiveCommandValidator : AbstractValidator<SetProviderStaffActiveCommand>
{
    public SetProviderStaffActiveCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
