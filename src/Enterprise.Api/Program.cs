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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Serilog;

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

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    // Composition root: each layer exposes exactly one `Add<Layer>()` extension method, so this
    // file stays a thin orchestrator instead of knowing which packages Application/Infrastructure
    // happen to use internally.
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddApiVersioningSetup();
    builder.Services.AddSwaggerSetup();
    builder.Services.AddApiLocalization();
    builder.Services.AddRateLimitingSetup();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Registered AFTER AddInfrastructure() so these override the default authorization policy
    // provider/handler set registered there - see PermissionPolicyProvider's remarks for why a
    // dynamic provider is used instead of pre-registering every permission as a named policy.
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, _, ex) =>
            ex is not null || httpContext.Response.StatusCode >= 500
                ? Serilog.Events.LogEventLevel.Error
                : httpContext.Response.StatusCode >= 400
                    ? Serilog.Events.LogEventLevel.Warning
                    : Serilog.Events.LogEventLevel.Verbose;
    });

    app.UseMiddleware<CorrelationIdMiddleware>();
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

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers().RequireRateLimiting(RateLimitingExtensions.GlobalPolicy);

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
