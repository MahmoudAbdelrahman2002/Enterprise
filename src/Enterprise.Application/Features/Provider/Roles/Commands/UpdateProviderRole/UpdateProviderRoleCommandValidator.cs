using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using Enterprise.Domain.Enums;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Roles.Commands.UpdateProviderRole;

public sealed class UpdateProviderRoleCommandValidator : AbstractValidator<UpdateProviderRoleCommand>
{
    public UpdateProviderRoleCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);

        RuleFor(x => x.Name)
            .NotNull()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .SetValidator(new LocalizedTextValidator(localizer, ValidationPolicy.RoleNameMax, englishRequired: true));

        RuleFor(x => x.Permissions)
            .NotNull()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .Must(p => p != null && p.Count > 0)
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);

        RuleFor(x => x.Permissions)
            .Must(values => values is null || values.Count == values.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            .WithMessage(_ => localizer[MessageKeys.Validation.DuplicatePermissions]);

        RuleForEach(x => x.Permissions)
            .Must(p => PermissionCatalog.IsValidForPortal(UserType.Provider, p))
            .WithMessage(_ => localizer[MessageKeys.Role.InvalidPermissions]);
    }
}
