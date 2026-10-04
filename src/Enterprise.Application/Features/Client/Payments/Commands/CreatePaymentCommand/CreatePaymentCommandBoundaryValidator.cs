using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.Payments.Commands.CreatePaymentCommand;

public sealed class CreatePaymentCommandBoundaryValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandBoundaryValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
    }
}
