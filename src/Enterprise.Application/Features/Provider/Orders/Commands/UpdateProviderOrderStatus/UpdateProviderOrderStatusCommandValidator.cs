using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Entities;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Orders.Commands.UpdateProviderOrderStatus;

public sealed class UpdateProviderOrderStatusCommandValidator : AbstractValidator<UpdateProviderOrderStatusCommand>
{
    public UpdateProviderOrderStatusCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.Status).IsInEnum().WithMessage(_ => localizer[MessageKeys.Validation.AllowedValue]);
    }
}
