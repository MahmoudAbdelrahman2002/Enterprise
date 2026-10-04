using Serilog;

namespace Enterprise.Api.Extensions;

public static class SerilogFileLoggingExtensions
{
    /// <summary>
    /// Azure App Service Kudu: site/wwwroot/log (ContentRoot).
    /// Local: {ContentRoot}/log.
    /// </summary>
    public static string ResolveFileLogDirectory(string contentRootPath)
    {
        return Path.Combine(contentRootPath, "log");
    }

    public static LoggerConfiguration WriteToRollingLogFile(
        this LoggerConfiguration configuration,
        string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);

        return configuration.WriteTo.File(
            path: Path.Combine(logDirectory, "log-.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
            outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
    }
}
