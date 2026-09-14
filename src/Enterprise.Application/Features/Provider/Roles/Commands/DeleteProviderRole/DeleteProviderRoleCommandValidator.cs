using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Roles.Commands.DeleteProviderRole;

public sealed class DeleteProviderRoleCommandValidator : AbstractValidator<DeleteProviderRoleCommand>
{
    public DeleteProviderRoleCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
