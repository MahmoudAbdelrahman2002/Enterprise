using Enterprise.Api.Extensions;
using FluentAssertions;
using Serilog;

namespace Enterprise.IntegrationTests;

[TestFixture]
public sealed class FileLoggingTests
{
    [Test]
    public void RollingFile_RecordsWarningAndAbove_InLogFolder()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "Enterprise.FileLoggingTests", Guid.NewGuid().ToString("N"));
        var logDirectory = SerilogFileLoggingExtensions.ResolveFileLogDirectory(contentRoot);

        try
        {
            using (var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteToRollingLogFile(logDirectory)
                .CreateLogger())
            {
                logger.Verbose("verbose-marker");
                logger.Debug("debug-marker");
                logger.Information("information-marker");
                logger.Warning("warning-marker");
                logger.Error(new InvalidOperationException("exception-marker"), "error-marker");
                logger.Fatal("fatal-marker");
            }

            logDirectory.Should().Be(Path.Combine(contentRoot, "log"));
            var files = Directory.GetFiles(logDirectory, "log-*.txt");
            files.Should().ContainSingle();
            var contents = File.ReadAllText(files[0]);
            contents.Should().Contain("warning-marker").And.Contain("error-marker")
                .And.Contain("fatal-marker").And.Contain("exception-marker");
            contents.Should().NotContain("verbose-marker").And.NotContain("debug-marker")
                .And.NotContain("information-marker");
        }
        finally
        {
            if (Directory.Exists(contentRoot))
                Directory.Delete(contentRoot, recursive: true);
        }
    }
}
