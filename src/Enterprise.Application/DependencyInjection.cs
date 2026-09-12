using System.Reflection;
using Enterprise.Application.Common.Behaviors;
using Enterprise.Application.Features.Auth.Common;
using FluentValidation;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.Application;

/// <summary>
/// Everything MediatR/FluentValidation/Mapster related is registered here, behind a single
/// <c>AddApplication()</c> call, so <c>Program.cs</c> never needs to know which packages the
/// Application layer happens to use internally - it just composes layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Order matters: behaviors wrap the handler in registration order, outermost first.
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });

        // LocalizedTextValidator is constructed manually with maxLength/englishRequired;
        // excluding it from the scan avoids DI failing on those non-service constructor args.
        services.AddValidatorsFromAssembly(
            assembly,
            filter: result => result.ValidatorType != typeof(Common.Validation.LocalizedTextValidator));

        TypeAdapterConfig.GlobalSettings.Scan(assembly);
        services.AddSingleton(TypeAdapterConfig.GlobalSettings);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<ITokenIssuanceService, TokenIssuanceService>();
        services.AddLocalization();

        return services;
    }
}
