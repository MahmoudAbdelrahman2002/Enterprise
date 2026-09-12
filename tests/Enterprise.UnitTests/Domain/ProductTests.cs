using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Enterprise.UnitTests.Domain;

[TestFixture]
public class ProductTests
{
    private static Product CreateProduct(int stock = 10) =>
        new("SKU-1", "Widget", "A widget", "Hardware", 9.99m, stock);

    [Test]
    public void Constructor_WithPositiveStock_SetsStatusActive()
    {
        var product = CreateProduct(stock: 5);

        product.Status.Should().Be(ProductStatus.Active);
    }

    [Test]
    public void Constructor_WithZeroStock_SetsStatusOutOfStock()
    {
        var product = CreateProduct(stock: 0);

        product.Status.Should().Be(ProductStatus.OutOfStock);
    }

    [Test]
    public void DecreaseStock_WithSufficientStock_ReducesQuantity()
    {
        var product = CreateProduct(stock: 10);

        product.DecreaseStock(4);

        product.StockQuantity.Should().Be(6);
        product.Status.Should().Be(ProductStatus.Active);
    }

    [Test]
    public void DecreaseStock_DownToZero_SetsStatusOutOfStock()
    {
        var product = CreateProduct(stock: 5);

        product.DecreaseStock(5);

        product.StockQuantity.Should().Be(0);
        product.Status.Should().Be(ProductStatus.OutOfStock);
    }

    [Test]
    public void DecreaseStock_MoreThanAvailable_ThrowsInsufficientStockException()
    {
        var product = CreateProduct(stock: 3);

        var act = () => product.DecreaseStock(4);

        act.Should().Throw<InsufficientStockException>()
            .Which.Should().BeAssignableTo<DomainException>();
    }

    [Test]
    public void DecreaseStock_NonPositiveQuantity_ThrowsArgumentOutOfRangeException()
    {
        var product = CreateProduct();

        var act = () => product.DecreaseStock(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void IncreaseStock_WhenOutOfStock_ReactivatesProduct()
    {
        var product = CreateProduct(stock: 0);

        product.IncreaseStock(2);

        product.StockQuantity.Should().Be(2);
        product.Status.Should().Be(ProductStatus.Active);
    }

    [Test]
    public void IncreaseStock_NonPositiveQuantity_ThrowsArgumentOutOfRangeException()
    {
        var product = CreateProduct();

        var act = () => product.IncreaseStock(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Discontinue_SetsStatusToDiscontinued()
    {
        var product = CreateProduct();

        product.Discontinue();

        product.Status.Should().Be(ProductStatus.Discontinued);
    }

    [Test]
    public void UpdateDetails_UpdatesMutableFieldsOnly()
    {
        var product = CreateProduct(stock: 7);

        product.UpdateDetails("New name", "New description", "New category", 19.99m);

        product.Name.Should().Be("New name");
        product.Description.Should().Be("New description");
        product.Category.Should().Be("New category");
        product.Price.Should().Be(19.99m);
        product.StockQuantity.Should().Be(7);
    }
}
