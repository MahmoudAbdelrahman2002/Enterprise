using System.Text;
using Asp.Versioning.ApiExplorer;
using Enterprise.Infrastructure.Identity.ApiKeyAuth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Enterprise.Api.Extensions;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerSetup(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.ConfigureOptions<ConfigureSwaggerOptions>();

        services.AddSwaggerGen(options =>
        {
            options.DocInclusionPredicate((documentName, apiDescription) =>
                string.Equals(apiDescription.GroupName, documentName, StringComparison.OrdinalIgnoreCase)
                || (documentName == "v1" && string.IsNullOrEmpty(apiDescription.GroupName)));

            options.OperationFilter<AcceptLanguageHeaderOperationFilter>();

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT access token."
            });

            options.AddSecurityDefinition(ApiKeyAuthenticationDefaults.SchemeName, new OpenApiSecurityScheme
            {
                Name = ApiKeyAuthenticationDefaults.HeaderName,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Service-to-service API key."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    []
                }
            });
        });

        return services;
    }

    /// <summary>
    /// One Swagger document PER discovered API version, rather than one document covering
    /// everything - so the UI accurately reflects "what does v1 look like" vs "what does v2
    /// look like" as the API evolves.
    /// </summary>
    public static WebApplication UseSwaggerSetup(this WebApplication app)
    {
        // Microsoft.OpenApi >= 1.6.23 serializes openapi: "3.0.4". Some Swagger UI builds reject
        // that version and show an empty definition even when paths exist. Rewrite to 3.0.1.
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var isSwaggerJson = path.StartsWith("/swagger/", StringComparison.OrdinalIgnoreCase)
                                && path.EndsWith("swagger.json", StringComparison.OrdinalIgnoreCase);

            if (!isSwaggerJson)
            {
                await next();
                return;
            }

            var originalBody = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await next();

                if (context.Response.StatusCode is < 200 or >= 300)
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody);
                    return;
                }

                buffer.Position = 0;
                var body = await new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true)
                    .ReadToEndAsync();
                body = body
                    .Replace("\"openapi\":\"3.0.4\"", "\"openapi\":\"3.0.1\"", StringComparison.Ordinal)
                    .Replace("\"openapi\": \"3.0.4\"", "\"openapi\": \"3.0.1\"", StringComparison.Ordinal);

                var bytes = Encoding.UTF8.GetBytes(body);
                context.Response.ContentLength = bytes.Length;
                await originalBody.WriteAsync(bytes);
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        });

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "Enterprise API";
            options.DocExpansion(DocExpansion.List);
            options.DefaultModelsExpandDepth(-1);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();

            var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            var descriptions = apiVersionDescriptionProvider.ApiVersionDescriptions.ToList();

            if (descriptions.Count == 0)
            {
                options.SwaggerEndpoint("v1/swagger.json", "Enterprise API V1");
            }
            else
            {
                foreach (var description in descriptions)
                {
                    options.SwaggerEndpoint(
                        $"{description.GroupName}/swagger.json",
                        $"Enterprise API {description.GroupName.ToUpperInvariant()}");
                }
            }
        });

        return app;
    }
}
