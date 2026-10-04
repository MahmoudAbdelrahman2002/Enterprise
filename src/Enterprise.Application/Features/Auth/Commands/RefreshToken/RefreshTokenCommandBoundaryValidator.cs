using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandBoundaryValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ExpectedUserType).IsInEnum().WithMessage(_ => localizer[MessageKeys.Validation.AllowedValue]);
    }
}
