using Enterprise.Api.Controllers;
using Enterprise.Api.Controllers.V1.Client;
using Enterprise.Api.Controllers.V1.Provider;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace Enterprise.IntegrationTests;

[TestFixture]
public sealed class ControllerNamingAndRoutesTests
{
    private static IEnumerable<(Type Controller, string Method, string Route)> Routes()
    {
        foreach (var controller in typeof(ApiControllerBase).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ApiControllerBase).IsAssignableFrom(type)))
        {
            var prefix = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? "";
            foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            foreach (var attribute in action.GetCustomAttributes<HttpMethodAttribute>())
            foreach (var method in attribute.HttpMethods)
            {
                var template = attribute.Template ?? "";
                var route = template.StartsWith("~/") ? template[2..] : template.StartsWith('/') ? template[1..] : (prefix + "/" + template).TrimEnd('/');
                yield return (controller, method, route);
            }
        }
    }

    [Test]
    public void CombinedControllers_PreserveExistingCollectionAndScopedRoutes()
    {
        const string prefix = "api/v{version:apiVersion}";
        var routes = Routes().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(routes, Does.Contain((typeof(ProviderProductsController), "GET", prefix + "/provider/products")));
            foreach (var method in new[] { "GET", "POST" })
                Assert.That(routes, Does.Contain((typeof(ProviderProductsController), method, prefix + "/provider/categories/{categoryId:guid}/products")));
            foreach (var method in new[] { "GET", "PUT", "DELETE" })
                Assert.That(routes, Does.Contain((typeof(ProviderProductsController), method, prefix + "/provider/categories/{categoryId:guid}/products/{id:guid}")));
            foreach (var method in new[] { "POST", "DELETE" })
                Assert.That(routes, Does.Contain((typeof(ProviderProductsController), method, prefix + "/provider/categories/{categoryId:guid}/products/{id:guid}/image")));
            Assert.That(routes, Does.Contain((typeof(ClientCartsController), "GET", prefix + "/client/carts")));
            foreach (var method in new[] { "GET", "DELETE" })
                Assert.That(routes, Does.Contain((typeof(ClientCartsController), method, prefix + "/client/{providerId:guid}/cart")));
            Assert.That(routes, Does.Contain((typeof(ClientCartsController), "POST", prefix + "/client/{providerId:guid}/cart/items")));
            foreach (var method in new[] { "PUT", "DELETE" })
                Assert.That(routes, Does.Contain((typeof(ClientCartsController), method, prefix + "/client/{providerId:guid}/cart/items/{cartItemId:guid}")));
        });
    }

    [Test]
    public void ResourceControllers_UsePluralNamesAndHaveNoDuplicateRoutes()
    {
        var routes = Routes().ToArray();
        var names = routes.Select(x => x.Controller.Name).Distinct().ToArray();
        foreach (var name in names)
            Assert.That(name, Does.Match("(?:s|Auth|Profile|Store|Staff)Controller$"), name);
        Assert.That(routes.GroupBy(x => (x.Method, x.Route)).Where(group => group.Count() > 1), Is.Empty);
    }
}
