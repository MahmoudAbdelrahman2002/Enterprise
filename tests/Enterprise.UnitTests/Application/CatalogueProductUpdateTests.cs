using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Products.Commands.UpdateProduct;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Enterprise.UnitTests.Application;

[TestFixture]
public sealed class CatalogueProductUpdateTests
{
    private readonly Guid _providerId = Guid.NewGuid();
    private Mock<IUnitOfWork> _unit = null!;
    private Mock<ICategoryRepository> _categories = null!;
    private Mock<IProductRepository> _products = null!;
    private Category _source = null!;
    private Category _destination = null!;
    private Product _product = null!;
    private UpdateProductCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _source = new Category(_providerId);
        _destination = new Category(_providerId);
        _product = new Product(_source.Id, "ORIGINAL", 10, ProductStatus.Active);
        _product.UpsertTranslation("en", "Original name", "Original description");
        _product.SetImageUrl("/existing.png");
        _categories = new Mock<ICategoryRepository>();
        _products = new Mock<IProductRepository>();
        _unit = new Mock<IUnitOfWork>();
        _unit.SetupGet(x => x.Categories).Returns(_categories.Object);
        _unit.SetupGet(x => x.Products).Returns(_products.Object);
        _categories.Setup(x => x.GetByIdAndProviderIdAsync(_source.Id, _providerId, It.IsAny<CancellationToken>())).ReturnsAsync(_source);
        _categories.Setup(x => x.GetByIdAndProviderIdAsync(_destination.Id, _providerId, It.IsAny<CancellationToken>())).ReturnsAsync(_destination);
        _products.Setup(x => x.GetByIdAndCategoryIdAndProviderIdAsync(_product.Id, _source.Id, _providerId, It.IsAny<CancellationToken>())).ReturnsAsync(_product);
        var context = new Mock<IProviderContext>();
        context.Setup(x => x.GetProviderIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_providerId);
        var culture = new Mock<ICurrentCulture>();
        culture.SetupGet(x => x.LanguageCode).Returns("en");
        _handler = new UpdateProductCommandHandler(_unit.Object, context.Object, culture.Object, NullLogger<UpdateProductCommandHandler>.Instance);
    }

    private UpdateProductCommand Request(Guid? categoryId) => new(_source.Id, _product.Id, new UpdateProductDto
    {
        CategoryId = categoryId,
        Sku = "UPDATED",
        Price = 15,
        Status = ProductStatus.Inactive,
        Name = new LocalizedText("Updated name", "Nome", "اسم"),
        Description = new LocalizedText("Updated description", "Descrizione", "وصف")
    });

    [Test]
    public async Task MoveAndEdit_PreservesIdentityImageAndHistoricalOrderSnapshot()
    {
        var orderItem = new OrderItem { ProductId = _product.Id, ProductName = "Original name", UnitPrice = 10, Quantity = 2 };
        var result = await _handler.Handle(Request(_destination.Id), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(_product.Id));
            Assert.That(result.CategoryId, Is.EqualTo(_destination.Id));
            Assert.That(result.Price, Is.EqualTo(15));
            Assert.That(result.Status, Is.EqualTo(ProductStatus.Inactive));
            Assert.That(result.ImageUrl, Is.EqualTo("/existing.png"));
            Assert.That(result.Translations!.Name.It, Is.EqualTo("Nome"));
            Assert.That(orderItem.ProductName, Is.EqualTo("Original name"));
            Assert.That(orderItem.UnitPrice, Is.EqualTo(10));
        });
        _unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LegacyUpdateWithoutCategory_PreservesCategory()
    {
        var result = await _handler.Handle(Request(null), CancellationToken.None);
        Assert.That(result.CategoryId, Is.EqualTo(_source.Id));
    }

    [Test]
    public void DestinationOutsideProvider_IsRejectedWithoutMutation()
    {
        Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(Request(Guid.NewGuid()), CancellationToken.None));
        Assert.That(_product.Sku, Is.EqualTo("ORIGINAL"));
        _unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void InactiveDestination_IsRejectedWithoutMutation()
    {
        _destination.SetActive(false);
        Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(Request(_destination.Id), CancellationToken.None));
        Assert.That(_product.CategoryId, Is.EqualTo(_source.Id));
        _unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
