using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Features.Products.Commands.UpdateProduct;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Enterprise.UnitTests.Handlers;

[TestFixture]
public class UpdateProductCommandHandlerTests
{
    private Mock<IProductRepository> _products = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private UpdateProductCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _products = new Mock<IProductRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.SetupGet(u => u.Products).Returns(_products.Object);
        _handler = new UpdateProductCommandHandler(_unitOfWork.Object);
    }

    [Test]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        _products.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        var command = new UpdateProductCommand(Guid.NewGuid(), "New name", null, "Category", 1m);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_ExistingProduct_UpdatesDetailsAndSaves()
    {
        var product = new Product("SKU-1", "Old name", "Old desc", "Old category", 5m, 10);
        _products.Setup(p => p.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        var command = new UpdateProductCommand(product.Id, "New name", "New desc", "New category", 15m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("New name");
        result.Description.Should().Be("New desc");
        result.Category.Should().Be("New category");
        result.Price.Should().Be(15m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
