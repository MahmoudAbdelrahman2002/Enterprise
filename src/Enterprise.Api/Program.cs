using Enterprise.Api.Authorization;
using Enterprise.Api.Extensions;
using Enterprise.Api.Middleware;
using Enterprise.Application;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Infrastructure;
using Enterprise.Infrastructure.BackgroundJobs;
using Enterprise.Infrastructure.BackgroundJobs.Jobs;
using Enterprise.Infrastructure.Identity;
using Enterprise.Infrastructure.Persistence;
using Enterprise.Infrastructure.Persistence.Seed;
using Hangfire;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Stripe;

// Bootstrap logger: captures any failure that happens before the full Serilog pipeline
// (which needs configuration/DI to be built) is up - otherwise a crash during startup would
// be silently swallowed instead of logged anywhere.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Enterprise.Api");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddApplicationInsightsTelemetry();

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        var logDirectory = SerilogFileLoggingExtensions.ResolveFileLogDirectory(context.HostingEnvironment.ContentRootPath);
        Directory.CreateDirectory(logDirectory);

        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .WriteToRollingLogFile(logDirectory);

        var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            var telemetryConfiguration = services.GetService<TelemetryConfiguration>()
                ?? TelemetryConfiguration.CreateDefault();
            if (string.IsNullOrWhiteSpace(telemetryConfiguration.ConnectionString))
            {
                telemetryConfiguration.ConnectionString = appInsightsConnectionString;
            }

            configuration.WriteTo.ApplicationInsights(
                telemetryConfiguration,
                TelemetryConverter.Traces);

            Log.Information("Serilog Application Insights sink enabled");
        }

        Log.Information("File logs directory: {LogDirectory}", logDirectory);
    });

    // Composition root: each layer exposes exactly one `Add<Layer>()` extension method, so this
    // file stays a thin orchestrator instead of knowing which packages Application/Infrastructure
    // happen to use internally.
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
    if (!string.IsNullOrWhiteSpace(stripeSecretKey))
    {
        StripeConfiguration.ApiKey = stripeSecretKey;
    }
    else
    {
        Log.Warning("Stripe payments are unavailable: configure Stripe:SecretKey using user secrets or the Stripe__SecretKey environment variable, then restart the backend");
    }

    builder.Services.AddControllers().AddValidationResponses();
    builder.Services.AddApiVersioningSetup();
    builder.Services.AddSwaggerSetup();
    builder.Services.AddApiLocalization();
    builder.Services.AddRateLimitingSetup();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SubitoWeb", policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:4200",
                    "https://localhost:4200",
                    "http://127.0.0.1:4200",
                    "https://127.0.0.1:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    
    // Registered AFTER AddInfrastructure() so these override the default authorization policy
    // provider/handler set registered there - see PermissionPolicyProvider's remarks for why a
    // dynamic provider is used instead of pre-registering every permission as a named policy.
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

    var app = builder.Build();

    // Correlation must wrap request logging so Serilog request completion logs include CorrelationId.
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, _, ex) =>
            ex is not null || httpContext.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode >= 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
    });

    app.UseExceptionHandler();
    app.UseApiLocalization();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseSwaggerSetup();

    if (app.Environment.IsDevelopment())
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
    }

    app.UseRateLimiter();

    app.UseCors("SubitoWeb");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers().RequireRateLimiting(RateLimitingExtensions.GlobalPolicy);

    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }

    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // liveness = "is the process up", no dependency checks
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        Authorization = [new HangfireDashboardAuthorizationFilter()]
    });

    using (var scope = app.Services.CreateScope())
    {
        // Migrate/seed before Hangfire touches SQL, otherwise a missing database
        // causes startup to fail while registering recurring jobs.
        if (app.Environment.IsDevelopment())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            await DbSeeder.SeedAsync(dbContext, userManager, roleManager, configuration, seedLogger);
        }

        var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
        backgroundJobService.AddOrUpdateRecurring<PurgeExpiredRefreshTokensJob>(
            "purge-expired-refresh-tokens",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily());
    }

    Log.Information(
        "File logs directory: {LogDirectory}",
        SerilogFileLoggingExtensions.ResolveFileLogDirectory(app.Environment.ContentRootPath));

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Enterprise.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposes the implicitly-generated Program class to WebApplicationFactory in the
/// integration test project, which needs a public/internal Program type to boot the app
/// in-memory.</summary>
public partial class Program;
