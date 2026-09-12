using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Features.Products.Commands.AdjustStock;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Exceptions;
using Enterprise.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Enterprise.UnitTests.Handlers;

[TestFixture]
public class AdjustStockCommandHandlerTests
{
    private Mock<IProductRepository> _products = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private AdjustStockCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _products = new Mock<IProductRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.SetupGet(u => u.Products).Returns(_products.Object);
        _handler = new AdjustStockCommandHandler(_unitOfWork.Object);
    }

    [Test]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        _products.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        var command = new AdjustStockCommand(Guid.NewGuid(), 5);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_PositiveDelta_IncreasesStock()
    {
        var product = new Product("SKU-1", "Widget", null, "Hardware", 9.99m, 5);
        _products.Setup(p => p.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        var command = new AdjustStockCommand(product.Id, 3);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.StockQuantity.Should().Be(8);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_NegativeDelta_DecreasesStock()
    {
        var product = new Product("SKU-1", "Widget", null, "Hardware", 9.99m, 5);
        _products.Setup(p => p.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        var command = new AdjustStockCommand(product.Id, -2);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.StockQuantity.Should().Be(3);
    }

    [Test]
    public async Task Handle_NegativeDeltaExceedingStock_ThrowsInsufficientStockException()
    {
        var product = new Product("SKU-1", "Widget", null, "Hardware", 9.99m, 2);
        _products.Setup(p => p.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        var command = new AdjustStockCommand(product.Id, -5);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientStockException>();
    }
}
