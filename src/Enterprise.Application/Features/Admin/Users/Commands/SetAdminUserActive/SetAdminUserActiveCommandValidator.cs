using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Users.Commands.SetAdminUserActive;

public sealed class SetAdminUserActiveCommandValidator : AbstractValidator<SetAdminUserActiveCommand>
{
    public SetAdminUserActiveCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
