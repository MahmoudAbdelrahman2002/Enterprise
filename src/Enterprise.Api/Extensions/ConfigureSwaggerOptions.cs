using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Enterprise.Api.Extensions;

/// <summary>
/// Registered as <c>IConfigureOptions&lt;SwaggerGenOptions&gt;</c> instead of configuring
/// <c>SwaggerGenOptions</c> directly inside <c>AddSwaggerGen(...)</c>, because the list of API
/// versions to generate a document for isn't known until <see cref="IApiVersionDescriptionProvider"/>
/// - built from the actual discovered controllers - is resolved from the DI container. This is
/// the standard Microsoft-documented pattern for combining Asp.Versioning with Swashbuckle.
/// </summary>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider apiVersionDescriptionProvider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            if (options.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey(description.GroupName))
            {
                continue;
            }

            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "Enterprise API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "This API version has been deprecated."
                    : "Production-ready enterprise API template."
            });
        }
    }
}
