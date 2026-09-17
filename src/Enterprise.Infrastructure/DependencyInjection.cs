using System.Text;
using Enterprise.Application.Common.Auth;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Settings;
using Enterprise.Domain.Interfaces;
using Enterprise.Infrastructure.BackgroundJobs;
using Enterprise.Infrastructure.Caching;
using Enterprise.Infrastructure.Common;
using Enterprise.Infrastructure.Email;
using Enterprise.Infrastructure.Identity;
using Enterprise.Infrastructure.Identity.ApiKeyAuth;
using Enterprise.Infrastructure.Identity.ApiKeyAuth.ExternalAuth;
using Enterprise.Infrastructure.Localization;
using Enterprise.Infrastructure.Providers;
using Enterprise.Infrastructure.Persistence;
using Enterprise.Infrastructure.Persistence.Interceptors;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Enterprise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddIdentity(services);
        AddCaching(services, configuration);
        AddAuthenticationAndAuthorization(services, configuration);
        AddBackgroundJobs(services, configuration);
        AddHealthChecks(services, configuration);

        services.AddHttpContextAccessor();
        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddSingleton<IAppLocalizer, AppLocalizer>();
        services.AddSingleton<ICurrentCulture, CurrentCulture>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IRoleManagerService, RoleManagerService>();
        services.AddScoped<IStaffManagerService, StaffManagerService>();
        services.AddScoped<IProviderAdminQueryService, ProviderAdminQueryService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();

        services.AddHttpClient(nameof(FacebookExternalAuthProvider), client =>
        {
            client.BaseAddress = new Uri("https://graph.facebook.com/v21.0/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IExternalAuthProvider, GoogleExternalAuthProvider>();
        services.AddScoped<IExternalAuthProvider, FacebookExternalAuthProvider>();
        services.AddScoped<IExternalAuthProviderResolver, ExternalAuthProviderResolver>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<AccountLockoutSettings>(configuration.GetSection(AccountLockoutSettings.SectionName));
        services.Configure<OtpSettings>(configuration.GetSection(OtpSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<DashboardUrlSettings>(configuration.GetSection(DashboardUrlSettings.SectionName));
        services.Configure<ExternalAuthSettings>(configuration.GetSection(ExternalAuthSettings.SectionName));

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddIdentity(IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, InMemoryCacheService>();
        }
    }

    private static void AddAuthenticationAndAuthorization(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.SchemeName, _ => { });

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(
                    JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthenticationDefaults.SchemeName)
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy("RequireClient", policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthenticationDefaults.SchemeName)
                .RequireAuthenticatedUser()
                .RequireClaim("user_type", nameof(Domain.Enums.UserType.Client)))
            .AddPolicy("RequireAdmin", policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthenticationDefaults.SchemeName)
                .RequireAuthenticatedUser()
                .RequireClaim("user_type", nameof(Domain.Enums.UserType.Admin)))
            .AddPolicy("RequireProvider", policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthenticationDefaults.SchemeName)
                .RequireAuthenticatedUser()
                .RequireClaim("user_type", nameof(Domain.Enums.UserType.Provider)));
    }

    private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        services.AddHangfireServer();
    }

    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks()
            .AddSqlServer(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "sql-server",
                tags: ["ready", "db"]);

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(redisConnectionString, name: "redis", tags: ["ready", "cache"]);
        }
    }
}
