using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Users.Commands.CreateAdminUser;

public sealed class CreateAdminUserCommandValidator : AbstractValidator<CreateAdminUserCommand>
{
    public CreateAdminUserCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.FirstName).Required(localizer).MaxLen(localizer, 50);
        RuleFor(x => x.LastName).Required(localizer).MaxLen(localizer, 50);
        RuleFor(x => x.Email).RequiredEmail(localizer).MaxLen(localizer, 256);
        RuleFor(x => x.Password).StrongPassword(localizer);
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
