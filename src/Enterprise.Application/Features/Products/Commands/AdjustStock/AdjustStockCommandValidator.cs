using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using FluentValidation;

namespace Enterprise.Application.Features.Products.Commands.AdjustStock;

public sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage(_ => localizer[MessageKeys.Validation.Required]);
        RuleFor(x => x.Delta)
            .NotEqual(0)
            .WithMessage(_ => localizer[MessageKeys.Validation.DeltaNonZero]);
    }
}
