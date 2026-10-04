using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Staff.Commands.UpdateProviderStaff;

public sealed class UpdateProviderStaffCommandValidator : AbstractValidator<UpdateProviderStaffCommand>
{
    public UpdateProviderStaffCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.FirstName).PersonName(localizer, ValidationPolicy.NameMax);
        RuleFor(x => x.LastName).PersonName(localizer, ValidationPolicy.NameMax);
        RuleFor(x => x.PhoneNumber).Phone(localizer);
        RuleFor(x => x.RoleId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
