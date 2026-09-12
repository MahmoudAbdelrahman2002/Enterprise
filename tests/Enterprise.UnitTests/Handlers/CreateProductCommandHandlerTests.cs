using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Features.Products.Commands.CreateProduct;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Enterprise.UnitTests.Handlers;

[TestFixture]
public class CreateProductCommandHandlerTests
{
    private Mock<IProductRepository> _products = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreateProductCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _products = new Mock<IProductRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.SetupGet(u => u.Products).Returns(_products.Object);
        _handler = new CreateProductCommandHandler(_unitOfWork.Object);
    }

    [Test]
    public async Task Handle_SkuAlreadyExists_ThrowsConflictException()
    {
        _products.Setup(p => p.SkuExistsAsync("SKU-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var command = new CreateProductCommand("SKU-1", "Widget", null, "Hardware", 9.99m, 10);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _products.Verify(p => p.Add(It.IsAny<Product>()), Times.Never);
    }

    [Test]
    public async Task Handle_NewSku_AddsProductAndSaves()
    {
        _products.Setup(p => p.SkuExistsAsync("SKU-1", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var command = new CreateProductCommand("SKU-1", "Widget", "desc", "Hardware", 9.99m, 10);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Sku.Should().Be("SKU-1");
        result.Name.Should().Be("Widget");
        result.StockQuantity.Should().Be(10);
        _products.Verify(p => p.Add(It.IsAny<Product>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
