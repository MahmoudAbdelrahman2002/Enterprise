using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Roles.Commands.DeleteAdminRole;

public sealed class DeleteAdminRoleCommandValidator : AbstractValidator<DeleteAdminRoleCommand>
{
    public DeleteAdminRoleCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
