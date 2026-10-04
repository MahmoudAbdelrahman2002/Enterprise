using Microsoft.Extensions.DependencyInjection;
using Enterprise.Application;
using Enterprise.Application.Common.Models;
using System.Text.Json;
using Enterprise.Application.Common.Images;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;
using Enterprise.Application.Features.Admin.Services.Commands.CreateMarketplaceService;
using Enterprise.Application.Features.Admin.Orders.Queries.GetAdminOrders;
using Enterprise.Application.Features.Provider.Products.Commands.CreateProduct;
using Enterprise.Application.Features.Provider.Products.DTOs;
using FluentValidation;
using Moq;

namespace Enterprise.UnitTests.Application;

[TestFixture]
public class ValidationPolicyTests
{
    private static IAppLocalizer Localizer()
    {
        var mock = new Mock<IAppLocalizer>();
        mock.Setup(x => x[It.IsAny<string>()]).Returns((string key) => key);
        mock.Setup(x => x[It.IsAny<string>(), It.IsAny<object[]>()]).Returns((string key, object[] args) => key);
        return mock.Object;
    }

    [Test]
    public void InputBearingRequests_HaveRegisteredValidators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Localizer());
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        var requests = typeof(ValidationPolicy).Assembly.GetTypes().Where(type => !type.IsAbstract && type.GetInterfaces().Any(contract =>
            contract == typeof(MediatR.IRequest) || (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>))) &&
            type.GetProperties().Any(property => property.Name != "IpAddress" && property.PropertyType != typeof(bool) && property.PropertyType != typeof(bool?)));
        foreach (var request in requests)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(request);
            Assert.That(provider.GetServices(validatorType).Any(), Is.True, request.FullName);
        }
    }

    [Test]
    public void SharedCases_MatchBackendFormats()
    {
        using var data = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "validation-cases.json")));
        foreach (var group in data.RootElement.EnumerateObject())
        foreach (var row in group.Value.EnumerateArray())
        {
            var actual = group.Name switch
            {
                "names" => FieldFormats.PersonName(row[0].GetString()),
                "emails" => FieldFormats.Email(row[0].GetString()),
                "phones" => FieldFormats.Phone(row[0].GetString()),
                "prices" => row[0].GetDecimal() >= .01m && row[0].GetDecimal() <= ValidationPolicy.PriceMax && decimal.Round(row[0].GetDecimal(), 2) == row[0].GetDecimal(),
                "otps" => System.Text.RegularExpressions.Regex.IsMatch(row[0].GetString()!, $"^[0-9]{{{ValidationPolicy.OtpLength}}}$"),
                _ => throw new InvalidOperationException(group.Name)
            };
            Assert.That(actual, Is.EqualTo(row[1].GetBoolean()), $"{group.Name}: {row[0]}");
        }
    }

    [Test]
    public async Task NullProduct_ReturnsValidationErrorInsteadOfThrowing()
    {
        var validator = new CreateProductCommandValidator(Localizer());
        var result = await validator.ValidateAsync(new CreateProductCommand(Guid.NewGuid(), null!));
        Assert.That(result.Errors.Any(error => error.PropertyName == "Product"), Is.True);
    }

    [TestCase("   ")]
    [TestCase("bad/code")]
    [TestCase("_code")]
    public void ServiceCode_RejectsBlankOrInvalidIdentifier(string code)
    {
        var result = new CreateMarketplaceServiceCommandValidator(Localizer()).Validate(new CreateMarketplaceServiceCommand(code, new LocalizedText("Pharmacy", null, null), null));
        Assert.That(result.IsValid, Is.False);
    }

    [TestCase(0)]
    [TestCase(101)]
    [TestCase(-1)]
    public void Paging_RejectsInvalidSizeWithoutChangingIt(int size)
    {
        var request = new GetAdminOrdersQuery { PageSize = size };
        Assert.That(request.PageSize, Is.EqualTo(size));
        Assert.That(new GetAdminOrdersQueryBoundaryValidator(Localizer()).Validate(request).IsValid, Is.False);
    }

    [Test]
    public void Queries_RejectReversedDatesUndefinedStatusAndOverflowPaging()
    {
        var validator = new GetAdminOrdersQueryBoundaryValidator(Localizer());
        Assert.Multiple(() =>
        {
            Assert.That(validator.Validate(new GetAdminOrdersQuery { From = DateTime.UtcNow, To = DateTime.UtcNow.AddDays(-1) }).IsValid, Is.False);
            Assert.That(validator.Validate(new GetAdminOrdersQuery { Status = (Enterprise.Domain.Entities.OrderStatus)999 }).IsValid, Is.False);
            Assert.That(validator.Validate(new GetAdminOrdersQuery { PageNumber = int.MaxValue, PageSize = 100 }).IsValid, Is.False);
        });
    }

    [Test]
    public async Task ImageChecks_AcceptPngAndPreserveStreamPosition_RejectSpoofedContent()
    {
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a9XkAAAAASUVORK5CYII=");
        using var stream = new MemoryStream(bytes);
        stream.Position = 2;
        var valid = new ImageUploadFile(stream, "image/png", "image.png", bytes.Length);
        Assert.That(await ImageUploadRules.ValidateContentAsync(valid, CancellationToken.None), Is.Null);
        Assert.That(stream.Position, Is.EqualTo(2));
        using var fake = new MemoryStream("not an image"u8.ToArray());
        Assert.That(await ImageUploadRules.ValidateContentAsync(new ImageUploadFile(fake, "image/png", "image.png", fake.Length), CancellationToken.None), Is.Not.Null);
    }
}
