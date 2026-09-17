using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Users.Commands.DeleteAdminUser;

public sealed class DeleteAdminUserCommandValidator : AbstractValidator<DeleteAdminUserCommand>
{
    public DeleteAdminUserCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
