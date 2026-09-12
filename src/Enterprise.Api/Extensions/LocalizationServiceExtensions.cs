using System.Globalization;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;

namespace Enterprise.Api.Extensions;

public static class LocalizationServiceExtensions
{
    public static readonly string[] SupportedCultureNames = ["en", "it", "ar"];

    public static IServiceCollection AddApiLocalization(this IServiceCollection services)
    {
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = SupportedCultureNames.Select(name => new CultureInfo(name)).ToList();
            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;
            options.ApplyCurrentCultureToResponseHeaders = true;
            options.RequestCultureProviders =
            [
                new AcceptLanguageHeaderRequestCultureProvider(),
                new QueryStringRequestCultureProvider
                {
                    QueryStringKey = "culture",
                    UIQueryStringKey = "ui-culture"
                }
            ];
        });

        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events ??= new JwtBearerEvents();
            options.Events.OnChallenge = WriteUnauthorizedAsync;
            options.Events.OnForbidden = WriteForbiddenAsync;
        });

        return services;
    }

    public static IApplicationBuilder UseApiLocalization(this IApplicationBuilder app) =>
        app.UseRequestLocalization();

    private static async Task WriteUnauthorizedAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        await WriteEnvelopeAsync(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            MessageKeys.Error.Unauthorized);
    }

    private static async Task WriteForbiddenAsync(ForbiddenContext context) =>
        await WriteEnvelopeAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            MessageKeys.Error.Forbidden);

    private static async Task WriteEnvelopeAsync(HttpContext httpContext, int statusCode, string messageKey)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var localizer = httpContext.RequestServices.GetRequiredService<IAppLocalizer>();
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object?>.Fail(
                statusCode,
                localizer[messageKey],
                traceId: httpContext.TraceIdentifier));
    }
}
