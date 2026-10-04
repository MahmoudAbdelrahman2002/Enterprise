using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Admin.Clients.Commands.SetAdminClientActive;

public sealed class SetAdminClientActiveCommandBoundaryValidator : AbstractValidator<SetAdminClientActiveCommand>
{
    public SetAdminClientActiveCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
