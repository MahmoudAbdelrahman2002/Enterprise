using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Users.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandValidator : AbstractValidator<UpdateAdminUserCommand>
{
    public UpdateAdminUserCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.FirstName).Required(localizer).MaxLen(localizer, 50);
        RuleFor(x => x.LastName).Required(localizer).MaxLen(localizer, 50);
        RuleFor(x => x.RoleId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
