using Enterprise.Domain.Entities;
using FluentValidation;

namespace Enterprise.Application.Features.Provider.Orders.Commands.UpdateProviderOrderStatus;

public sealed class UpdateProviderOrderStatusCommandValidator : AbstractValidator<UpdateProviderOrderStatusCommand>
{
    public UpdateProviderOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
