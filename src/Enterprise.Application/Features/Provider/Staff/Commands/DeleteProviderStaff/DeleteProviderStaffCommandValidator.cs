using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Staff.Commands.DeleteProviderStaff;

public sealed class DeleteProviderStaffCommandValidator : AbstractValidator<DeleteProviderStaffCommand>
{
    public DeleteProviderStaffCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
