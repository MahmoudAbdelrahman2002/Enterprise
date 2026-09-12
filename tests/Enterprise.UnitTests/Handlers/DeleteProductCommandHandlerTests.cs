using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Features.Products.Commands.DeleteProduct;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Enterprise.UnitTests.Handlers;

[TestFixture]
public class DeleteProductCommandHandlerTests
{
    private Mock<IProductRepository> _products = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private DeleteProductCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _products = new Mock<IProductRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.SetupGet(u => u.Products).Returns(_products.Object);
        _handler = new DeleteProductCommandHandler(_unitOfWork.Object);
    }

    [Test]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        _products.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _handler.Handle(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_ExistingProduct_RemovesAndSaves()
    {
        var product = new Product("SKU-1", "Widget", null, "Hardware", 9.99m, 10);
        _products.Setup(p => p.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await _handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        _products.Verify(p => p.Remove(product), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
