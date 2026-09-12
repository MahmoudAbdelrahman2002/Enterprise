using Asp.Versioning;

namespace Enterprise.Api.Extensions;

public static class VersioningServiceExtensions
{
    public static IServiceCollection AddApiVersioningSetup(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;

                // URL segment ("/api/v1/products") over header/query-string versioning: it's
                // visible in every log line, curl command and browser address bar without
                // needing to know an implicit convention - the most discoverable option for an
                // API that expects external/third-party consumers.
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
