using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using Enterprise.Domain.Enums;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Roles.Commands.CreateProviderRole;

public sealed class CreateProviderRoleCommandValidator : AbstractValidator<CreateProviderRoleCommand>
{
    public CreateProviderRoleCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Name)
            .Required(localizer)
            .MaxLen(localizer, 100);

        RuleFor(x => x.Permissions)
            .NotNull()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .Must(p => p != null && p.Count > 0)
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);

        RuleForEach(x => x.Permissions)
            .Must(p => PermissionCatalog.IsValidForPortal(UserType.Provider, p))
            .WithMessage(_ => localizer[MessageKeys.Role.InvalidPermissions]);
    }
}
