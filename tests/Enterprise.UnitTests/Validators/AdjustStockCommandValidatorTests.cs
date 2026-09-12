using Enterprise.Application.Features.Products.Commands.AdjustStock;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Enterprise.UnitTests.Validators;

[TestFixture]
public class AdjustStockCommandValidatorTests
{
    private readonly AdjustStockCommandValidator _validator = new();

    [Test]
    public void ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(new AdjustStockCommand(Guid.NewGuid(), 5));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void ZeroDelta_FailsValidation()
    {
        var result = _validator.TestValidate(new AdjustStockCommand(Guid.NewGuid(), 0));

        result.ShouldHaveValidationErrorFor(x => x.Delta);
    }

    [Test]
    public void EmptyProductId_FailsValidation()
    {
        var result = _validator.TestValidate(new AdjustStockCommand(Guid.Empty, 5));

        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }
}
