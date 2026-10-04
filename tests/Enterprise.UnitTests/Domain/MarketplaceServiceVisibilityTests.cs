using Enterprise.Domain.Entities;
using Enterprise.Domain.Specifications.Services;

namespace Enterprise.UnitTests.Domain;

[TestFixture]
public class MarketplaceServiceVisibilityTests
{
    [Test]
    public void CustomerCriteria_ExcludeServicesWithoutActiveProviders()
    {
        var available = new MarketplaceService("restaurant");
        var empty = new MarketplaceService("pharmacy");
        var inactive = new MarketplaceService("grocery", isActive: false);
        IReadOnlyList<Guid> activeProviderServices = [available.Id, inactive.Id];
        var count = MarketplaceServiceFilterSpecification.ForCount(null, true, "en", activeProviderServices).Criteria!.Compile();
        var page = MarketplaceServiceFilterSpecification.ForPage(null, true, 1, 20, "en", activeProviderServices).Criteria!.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(count(available), Is.True);
            Assert.That(page(available), Is.True);
            Assert.That(count(empty), Is.False);
            Assert.That(page(empty), Is.False);
            Assert.That(count(inactive), Is.False);
            Assert.That(page(inactive), Is.False);
        });
    }

    [Test]
    public void NoActiveProviders_CustomerCatalogIsEmpty()
    {
        var criteria = MarketplaceServiceFilterSpecification.ForCount(null, true, "en", []).Criteria!.Compile();
        Assert.That(criteria(new MarketplaceService("pharmacy")), Is.False);
    }

    [Test]
    public void AdminCriteria_StillIncludeEmptyAndInactiveServices()
    {
        var criteria = MarketplaceServiceFilterSpecification.ForCount(null, null, "en").Criteria!.Compile();
        Assert.That(criteria(new MarketplaceService("pharmacy", isActive: false)), Is.True);
    }
}
