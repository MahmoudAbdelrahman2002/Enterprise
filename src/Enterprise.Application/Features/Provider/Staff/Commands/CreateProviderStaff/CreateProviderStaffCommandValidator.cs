using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Staff.Commands.CreateProviderStaff;

public sealed class CreateProviderStaffCommandValidator : AbstractValidator<CreateProviderStaffCommand>
{
    public CreateProviderStaffCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.FirstName).PersonName(localizer, ValidationPolicy.NameMax);
        RuleFor(x => x.LastName).PersonName(localizer, ValidationPolicy.NameMax);
        RuleFor(x => x.Email).RequiredEmail(localizer);
        RuleFor(x => x.Password).StrongPassword(localizer);
        RuleFor(x => x.PhoneNumber).Phone(localizer);
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
