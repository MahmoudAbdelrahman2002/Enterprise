using Enterprise.Application.Features.Products.Commands.CreateProduct;
using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Enterprise.UnitTests.Validators;

[TestFixture]
public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    private static CreateProductCommand ValidCommand() =>
        new("SKU-100", "Widget", "A widget", "Hardware", 9.99m, 10);

    [Test]
    public void ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void EmptySku_FailsValidation()
    {
        var command = ValidCommand() with { Sku = string.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Sku);
    }

    [Test]
    public void SkuWithInvalidCharacters_FailsValidation()
    {
        var command = ValidCommand() with { Sku = "SKU 100!" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Sku);
    }

    [Test]
    public void NonPositivePrice_FailsValidation()
    {
        var command = ValidCommand() with { Price = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Test]
    public void NegativeStockQuantity_FailsValidation()
    {
        var command = ValidCommand() with { StockQuantity = -1 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StockQuantity);
    }

    [Test]
    public void EmptyName_FailsValidation()
    {
        var command = ValidCommand() with { Name = string.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
